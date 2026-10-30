namespace FamVault.Modules.Identity.Application;

public sealed record SetupStatusResponse(bool NeedsSetup);

public sealed record SetupRequest(string UserName, string DisplayName, string Password);

public sealed record LoginRequest(string UserName, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    UserDto User);

public sealed record UserDto(
    Guid Id,
    string UserName,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateUserRequest(string UserName, string DisplayName, string Password, string Role);

public sealed record UpdateUserRequest(string DisplayName, string Role, bool IsActive);

public sealed record ResetPasswordRequest(string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
