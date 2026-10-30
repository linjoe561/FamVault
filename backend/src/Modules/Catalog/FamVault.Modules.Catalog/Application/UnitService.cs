using FamVault.Modules.Catalog.Domain;
using FamVault.Modules.Catalog.Infrastructure;
using FamVault.SharedKernel.Errors;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Catalog.Application;

public sealed class UnitService(CatalogModuleDbContext db)
{
    public async Task<IReadOnlyList<UnitDto>> ListAsync(CancellationToken cancellationToken) =>
        await db.Units
            .OrderBy(u => u.SortOrder).ThenBy(u => u.Name)
            .Select(u => new UnitDto(u.Id, u.Name, db.Items.Count(i => i.UnitId == u.Id)))
            .ToListAsync(cancellationToken);

    public async Task<UnitDto> CreateAsync(SaveUnitRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureNameAvailableAsync(name, exceptId: null, cancellationToken);

        var unit = new Unit
        {
            Name = name,
            SortOrder = await db.Units.MaxAsync(u => (int?)u.SortOrder, cancellationToken) + 1 ?? 1,
        };
        db.Units.Add(unit);
        await db.SaveChangesAsync(cancellationToken);
        return new UnitDto(unit.Id, unit.Name, 0);
    }

    public async Task<UnitDto> UpdateAsync(int id, SaveUnitRequest request, CancellationToken cancellationToken)
    {
        var unit = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureNameAvailableAsync(name, id, cancellationToken);

        unit.Name = name;
        await db.SaveChangesAsync(cancellationToken);
        return new UnitDto(unit.Id, unit.Name, await db.Items.CountAsync(i => i.UnitId == id, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var unit = await FindAsync(id, cancellationToken);
        var itemCount = await db.Items.CountAsync(i => i.UnitId == id, cancellationToken);
        if (itemCount > 0)
        {
            throw new ConflictException($"還有 {itemCount} 個品項使用「{unit.Name}」這個單位，請先改成其他單位");
        }

        db.Units.Remove(unit);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Unit> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Units.FindAsync([id], cancellationToken) ?? throw new NotFoundException("找不到這個單位");

    private async Task EnsureNameAvailableAsync(string name, int? exceptId, CancellationToken cancellationToken)
    {
        if (await db.Units.AnyAsync(u => u.Name == name && u.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"已經有叫「{name}」的單位");
        }
    }
}
