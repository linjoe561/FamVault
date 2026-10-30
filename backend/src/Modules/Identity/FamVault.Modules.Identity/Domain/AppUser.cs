using FamVault.SharedKernel.Auth;
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
    public const string Admin = AppRoles.Admin;
    public const string Member = AppRoles.Member;

    public static readonly IReadOnlyList<string> All = [Admin, Member];
}
