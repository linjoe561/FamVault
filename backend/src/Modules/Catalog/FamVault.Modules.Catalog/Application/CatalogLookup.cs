using FamVault.Modules.Catalog.Domain;
using FamVault.Modules.Catalog.Infrastructure;
using FamVault.SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Catalog.Application;

internal sealed class CatalogLookup(CatalogModuleDbContext db) : ICatalogLookup
{
    public async Task<IReadOnlyList<CatalogItemInfo>> FindItemsAsync(
        CatalogItemFilter filter, CancellationToken cancellationToken = default)
    {
        var items = ItemFilters.Apply(db.Items.AsNoTracking(), filter.Search, filter.CategoryId, filter.IncludeArchived);
        if (filter.Ids is { } ids)
        {
            items = items.Where(i => ids.Contains(i.Id));
        }

        return await items
            .OrderBy(i => i.Name)
            .Select(i => new CatalogItemInfo(
                i.Id,
                i.Name,
                i.CategoryId,
                i.Category != null ? i.Category.Name : null,
                i.Unit.Name,
                i.LocationId,
                i.SafetyStock,
                i.ShelfLifeDays,
                i.IsArchived))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetLocationNamesAsync(CancellationToken cancellationToken = default) =>
        await db.Locations.ToDictionaryAsync(l => l.Id, l => l.Name, cancellationToken);
}

/// <summary>品項的共用篩選，ItemService 與 CatalogLookup 共用，確保兩邊搜尋結果一致。</summary>
internal static class ItemFilters
{
    public static IQueryable<Item> Apply(IQueryable<Item> items, string? search, int? categoryId, bool includeArchived)
    {
        if (!includeArchived)
        {
            items = items.Where(i => !i.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            var code = text.ToUpperInvariant();
            items = items.Where(i => i.Name.Contains(text) || i.Barcodes.Any(b => b.Code == code));
        }

        if (categoryId is { } id)
        {
            // 選大分類時，也包含底下小分類的品項
            items = items.Where(i => i.CategoryId == id || i.Category!.ParentId == id);
        }

        return items;
    }
}
