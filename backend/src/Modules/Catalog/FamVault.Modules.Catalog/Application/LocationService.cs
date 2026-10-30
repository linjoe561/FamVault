using FamVault.Modules.Catalog.Domain;
using FamVault.Modules.Catalog.Infrastructure;
using FamVault.SharedKernel.Errors;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Catalog.Application;

public sealed class LocationService(CatalogModuleDbContext db)
{
    public async Task<IReadOnlyList<LocationDto>> ListAsync(CancellationToken cancellationToken) =>
        await db.Locations
            .OrderBy(l => l.SortOrder).ThenBy(l => l.Name)
            .Select(l => new LocationDto(l.Id, l.Name, l.Description, db.Items.Count(i => i.LocationId == l.Id)))
            .ToListAsync(cancellationToken);

    public async Task<LocationDto> CreateAsync(SaveLocationRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureNameAvailableAsync(name, exceptId: null, cancellationToken);

        var location = new Location
        {
            Name = name,
            Description = NullIfBlank(request.Description),
            SortOrder = await db.Locations.MaxAsync(l => (int?)l.SortOrder, cancellationToken) + 1 ?? 1,
        };
        db.Locations.Add(location);
        await db.SaveChangesAsync(cancellationToken);
        return new LocationDto(location.Id, location.Name, location.Description, 0);
    }

    public async Task<LocationDto> UpdateAsync(int id, SaveLocationRequest request, CancellationToken cancellationToken)
    {
        var location = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureNameAvailableAsync(name, id, cancellationToken);

        location.Name = name;
        location.Description = NullIfBlank(request.Description);
        await db.SaveChangesAsync(cancellationToken);
        return new LocationDto(
            location.Id, location.Name, location.Description,
            await db.Items.CountAsync(i => i.LocationId == id, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var location = await FindAsync(id, cancellationToken);
        var itemCount = await db.Items.CountAsync(i => i.LocationId == id, cancellationToken);
        if (itemCount > 0)
        {
            throw new ConflictException($"還有 {itemCount} 個品項預設放在「{location.Name}」，請先改成其他位置");
        }

        db.Locations.Remove(location);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Location> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Locations.FindAsync([id], cancellationToken) ?? throw new NotFoundException("找不到這個位置");

    private async Task EnsureNameAvailableAsync(string name, int? exceptId, CancellationToken cancellationToken)
    {
        if (await db.Locations.AnyAsync(l => l.Name == name && l.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"已經有叫「{name}」的位置");
        }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
