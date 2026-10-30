using FamVault.Modules.Identity.Domain;
using FamVault.Modules.Identity.Infrastructure;
using FamVault.SharedKernel.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Identity.Application;

/// <summary>家庭成員管理（管理者使用）。</summary>
public sealed class UserService(
    UserManager<AppUser> userManager,
    IdentityModuleDbContext db,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().OrderBy(u => u.CreatedAt).ToListAsync(cancellationToken);
        var roles = await (
                from userRole in db.UserRoles
                join role in db.Roles on userRole.RoleId equals role.Id
                select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);
        var rolesByUser = roles.ToLookup(r => r.UserId, r => r.Name!);

        return users.Select(u => ToDto(u, rolesByUser[u.Id])).ToList();
    }

    public async Task<UserDto> GetAsync(Guid id)
    {
        var user = await FindAsync(id);
        return ToDto(user, await userManager.GetRolesAsync(user));
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var user = await CreateUserAsync(request.UserName, request.DisplayName, request.Password, request.Role);
        return ToDto(user, [request.Role]);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await FindAsync(id);
        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Contains(RoleNames.Admin) && (request.Role != RoleNames.Admin || !request.IsActive))
        {
            await EnsureAnotherActiveAdminAsync(user.Id);
        }

        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        EnsureSucceeded(await userManager.UpdateAsync(user));

        if (!currentRoles.SequenceEqual([request.Role]))
        {
            EnsureSucceeded(await userManager.RemoveFromRolesAsync(user, currentRoles));
            EnsureSucceeded(await userManager.AddToRoleAsync(user, request.Role));
        }

        if (!request.IsActive)
        {
            await RevokeAllRefreshTokensAsync(user.Id);
        }

        return ToDto(user, [request.Role]);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request)
    {
        var user = await FindAsync(id);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        EnsureSucceeded(await userManager.ResetPasswordAsync(user, token, request.NewPassword));
        await RevokeAllRefreshTokensAsync(user.Id);
    }

    internal async Task<AppUser> CreateUserAsync(string userName, string displayName, string password, string role)
    {
        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = userName.Trim(),
            DisplayName = displayName.Trim(),
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow(),
        };
        EnsureSucceeded(await userManager.CreateAsync(user, password));
        EnsureSucceeded(await userManager.AddToRoleAsync(user, role));
        return user;
    }

    internal async Task RevokeAllRefreshTokensAsync(Guid userId)
    {
        var now = timeProvider.GetUtcNow();
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
    }

    internal static UserDto ToDto(AppUser user, IEnumerable<string> roles) => new(
        user.Id,
        user.UserName ?? string.Empty,
        user.DisplayName,
        roles.Contains(RoleNames.Admin) ? RoleNames.Admin : RoleNames.Member,
        user.IsActive,
        user.CreatedAt);

    internal static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new BusinessRuleException(string.Join("；", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task<AppUser> FindAsync(Guid id) =>
        await userManager.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("找不到這位成員");

    private async Task EnsureAnotherActiveAdminAsync(Guid userId)
    {
        var admins = await userManager.GetUsersInRoleAsync(RoleNames.Admin);
        if (!admins.Any(a => a.Id != userId && a.IsActive))
        {
            throw new BusinessRuleException("至少要保留一位啟用中的管理者");
        }
    }
}
