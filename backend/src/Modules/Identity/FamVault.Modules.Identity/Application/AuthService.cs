using FamVault.Modules.Identity.Domain;
using FamVault.Modules.Identity.Infrastructure;
using FamVault.SharedKernel.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamVault.Modules.Identity.Application;

/// <summary>初始設定、登入、換發與登出。</summary>
public sealed class AuthService(
    UserManager<AppUser> userManager,
    UserService userService,
    IdentityModuleDbContext db,
    TokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider)
{
    private const string InvalidCredentials = "帳號或密碼錯誤";
    private const string SessionExpired = "登入已失效，請重新登入";

    public async Task<bool> NeedsSetupAsync(CancellationToken cancellationToken) =>
        !await db.Users.AnyAsync(cancellationToken);

    /// <summary>系統第一次使用時建立第一位管理者。已經有成員後就不能再呼叫。</summary>
    public async Task<AuthResponse> SetupAsync(SetupRequest request, CancellationToken cancellationToken)
    {
        if (!await NeedsSetupAsync(cancellationToken))
        {
            throw new ConflictException("系統已完成初始設定");
        }

        var user = await userService.CreateUserAsync(
            request.UserName, request.DisplayName, request.Password, RoleNames.Admin);
        return await IssueAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.UserName.Trim());
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException(InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("登入失敗次數過多，請 5 分鐘後再試");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            throw new UnauthorizedException(InvalidCredentials);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await IssueAsync(user, cancellationToken);
    }

    /// <summary>
    /// 用 Refresh Token 換一組新的憑證，舊的同時撤銷。
    /// 若收到已撤銷的 Token（可能被盜用後重放），撤銷該成員所有 Token，強制重新登入。
    /// </summary>
    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var hash = TokenService.HashRefreshToken(request.RefreshToken ?? string.Empty);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new UnauthorizedException(SessionExpired);

        if (stored.RevokedAt is not null)
        {
            await userService.RevokeAllRefreshTokensAsync(stored.UserId);
            throw new UnauthorizedException(SessionExpired);
        }

        var now = timeProvider.GetUtcNow();
        if (stored.ExpiresAt <= now)
        {
            throw new UnauthorizedException(SessionExpired);
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException(SessionExpired);
        }

        stored.RevokedAt = now;
        return await IssueAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var hash = TokenService.HashRefreshToken(request.RefreshToken ?? string.Empty);
        var now = timeProvider.GetUtcNow();
        await db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("找不到這位成員");
        UserService.EnsureSucceeded(
            await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword));
    }

    private async Task<AuthResponse> IssueAsync(AppUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);
        var refreshToken = TokenService.GenerateRefreshToken();
        var now = timeProvider.GetUtcNow();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = TokenService.HashRefreshToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays),
        });
        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken.Token, accessToken.ExpiresAt, refreshToken, UserService.ToDto(user, roles));
    }
}
