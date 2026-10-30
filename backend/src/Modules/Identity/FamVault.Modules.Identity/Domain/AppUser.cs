using Microsoft.AspNetCore.Identity;

namespace FamVault.Modules.Identity.Domain;

/// <summary>家庭成員帳號。用帳號（UserName）登入，不強制 Email。</summary>
public sealed class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AppRole : IdentityRole<Guid>
{
}

public static class RoleNames
{
    /// <summary>可以管理家庭成員與主檔資料。</summary>
    public const string Admin = "Admin";

    /// <summary>一般成員：登錄購買、使用與查詢庫存。</summary>
    public const string Member = "Member";

    public static readonly IReadOnlyList<string> All = [Admin, Member];
}
