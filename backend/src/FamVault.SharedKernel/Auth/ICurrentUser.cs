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
/// 角色名稱。放在 SharedKernel，讓各模組的 [Authorize(Roles = ...)] 不必參照 Identity 模組。
/// </summary>
public static class AppRoles
{
    /// <summary>可以管理家庭成員與主檔資料（分類、位置、單位）。</summary>
    public const string Admin = "Admin";

    /// <summary>一般成員：登錄購買、使用與查詢庫存，可以新增與修改品項。</summary>
    public const string Member = "Member";
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
