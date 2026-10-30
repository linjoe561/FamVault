namespace FamVault.SharedKernel.Auth;

/// <summary>
/// 目前發出請求的使用者。沒有登入（或在背景工作中）時 UserId 為 null。
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? DisplayName { get; }
    bool IsInRole(string role);
}

/// <summary>
/// JWT 內使用的 claim 名稱，簽發與驗證兩端共用。
/// </summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string UserName = "preferred_username";
    public const string DisplayName = "name";
    public const string Role = "role";
}
