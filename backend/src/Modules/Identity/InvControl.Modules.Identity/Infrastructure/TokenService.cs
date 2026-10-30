using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InvControl.Modules.Identity.Domain;
using InvControl.SharedKernel.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InvControl.Modules.Identity.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "InvControl";
    public string Audience { get; set; } = "InvControl";

    /// <summary>HMAC 簽章金鑰，至少 32 個字元。正式環境請用環境變數或 user-secrets 設定。</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}

public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public IssuedAccessToken CreateAccessToken(AppUser user, IEnumerable<string> roles)
    {
        var jwt = options.Value;
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId, user.Id.ToString()),
            new(AppClaimTypes.UserName, user.UserName ?? string.Empty),
            new(AppClaimTypes.DisplayName, user.DisplayName),
        };
        claims.AddRange(roles.Select(role => new Claim(AppClaimTypes.Role, role)));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = timeProvider.GetUtcNow().UtcDateTime,
            NotBefore = timeProvider.GetUtcNow().UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });

        return new IssuedAccessToken(token, expiresAt);
    }

    public static string GenerateRefreshToken() =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    public static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
