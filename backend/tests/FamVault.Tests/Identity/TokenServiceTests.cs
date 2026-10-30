using FamVault.Modules.Identity.Domain;
using FamVault.Modules.Identity.Infrastructure;
using FamVault.SharedKernel.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FamVault.Tests.Identity;

public class TokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = new string('k', 48),
        AccessTokenMinutes = 30,
    };

    [Fact]
    public async Task CreateAccessToken_contains_user_claims_and_validates_with_same_key()
    {
        var service = new TokenService(Microsoft.Extensions.Options.Options.Create(Options), TimeProvider.System);
        var user = new AppUser { Id = Guid.NewGuid(), UserName = "mom", DisplayName = "媽媽" };

        var issued = service.CreateAccessToken(user, [RoleNames.Admin]);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(issued.Token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = Options.CreateSigningKey(),
        });

        Assert.True(result.IsValid);
        Assert.Equal(user.Id.ToString(), result.Claims[AppClaimTypes.UserId]);
        Assert.Equal("媽媽", result.Claims[AppClaimTypes.DisplayName]);
        Assert.Equal(RoleNames.Admin, result.Claims[AppClaimTypes.Role]);
    }

    [Fact]
    public void RefreshToken_is_random_and_hash_is_stable()
    {
        var first = TokenService.GenerateRefreshToken();
        var second = TokenService.GenerateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.Equal(TokenService.HashRefreshToken(first), TokenService.HashRefreshToken(first));
        Assert.Equal(64, TokenService.HashRefreshToken(first).Length);
    }
}
