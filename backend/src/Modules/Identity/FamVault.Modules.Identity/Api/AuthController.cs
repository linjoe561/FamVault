using FamVault.Modules.Identity.Application;
using FamVault.SharedKernel.Auth;
using FamVault.SharedKernel.Errors;
using FamVault.SharedKernel.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamVault.Modules.Identity.Api;

[ApiController]
[Route(ApiRoutes.Prefix + "/auth")]
public sealed class AuthController(AuthService authService, UserService userService, ICurrentUser currentUser)
    : ControllerBase
{
    /// <summary>是否還沒有任何成員（需要先建立第一位管理者）。</summary>
    [AllowAnonymous]
    [HttpGet("setup-status")]
    public async Task<SetupStatusResponse> GetSetupStatus(CancellationToken cancellationToken) =>
        new(await authService.NeedsSetupAsync(cancellationToken));

    [AllowAnonymous]
    [HttpPost("setup")]
    public Task<AuthResponse> Setup(SetupRequest request, CancellationToken cancellationToken) =>
        authService.SetupAsync(request, cancellationToken);

    [AllowAnonymous]
    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);

    [AllowAnonymous]
    [HttpPost("refresh")]
    public Task<AuthResponse> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        authService.RefreshAsync(request, cancellationToken);

    /// <summary>撤銷 Refresh Token。Access Token 可能已過期，所以不需要登入也能呼叫。</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    public Task<UserDto> Me() => userService.GetAsync(RequireUserId());

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        await authService.ChangePasswordAsync(RequireUserId(), request);
        return NoContent();
    }

    private Guid RequireUserId() =>
        currentUser.UserId ?? throw new UnauthorizedException("請先登入");
}
