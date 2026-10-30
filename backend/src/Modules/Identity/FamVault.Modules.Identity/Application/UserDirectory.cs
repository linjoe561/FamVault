using FamVault.Modules.Identity.Infrastructure;
using FamVault.SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Identity.Application;

internal sealed class UserDirectory(IdentityModuleDbContext db) : IUserDirectory
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        return await db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);
    }
}
