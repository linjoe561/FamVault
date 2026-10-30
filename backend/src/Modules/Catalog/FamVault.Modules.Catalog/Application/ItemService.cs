using FamVault.Modules.Catalog.Domain;
using FamVault.Modules.Catalog.Infrastructure;
using FamVault.SharedKernel.Contracts;
using FamVault.SharedKernel.Errors;
using FamVault.SharedKernel.Web;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Catalog.Application;

public sealed class ItemService(CatalogModuleDbContext db, IEnumerable<ICatalogReferenceChecker> referenceCheckers)
{
    public async Task<PagedResult<ItemDto>> ListAsync(ItemQuery query, CancellationToken cancellationToken)
    {
        var items = ItemFilters.Apply(db.Items.AsNoTracking(), query.Search, query.CategoryId, query.IncludeArchived);

        if (query.LocationId is { } locationId)
        {
            items = items.Where(i => i.LocationId == locationId);
        }

        var total = await items.CountAsync(cancellationToken);
        var page = await ProjectToDto(items.OrderBy(i => i.Name))
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ItemDto>(page, total, query.Page, query.PageSize);
    }

    public async Task<ItemDto> GetAsync(int id, CancellationToken cancellationToken) =>
        await ProjectToDto(db.Items.Where(i => i.Id == id)).SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("找不到這個品項");

    /// <summary>掃條碼時用：找不到回傳 404，前端就引導新增品項並綁定這個條碼。</summary>
    public async Task<ItemDto> GetByBarcodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = NormalizeBarcode(code);
        return await ProjectToDto(db.Items.Where(i => i.Barcodes.Any(b => b.Code == normalized)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"還沒有品項使用條碼 {normalized}");
    }

    public async Task<ItemDto> CreateAsync(SaveItemRequest request, CancellationToken cancellationToken)
    {
        var item = new Item();
        await ApplyAsync(item, request, cancellationToken);
        db.Items.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(item.Id, cancellationToken);
    }

    public async Task<ItemDto> UpdateAsync(int id, SaveItemRequest request, CancellationToken cancellationToken)
    {
        var item = await db.Items.Include(i => i.Barcodes).SingleOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("找不到這個品項");
        await ApplyAsync(item, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>永久刪除。有庫存紀錄等其他模組資料時不能刪除，只能停用。</summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var item = await db.Items.FindAsync([id], cancellationToken) ?? throw new NotFoundException("找不到這個品項");
        foreach (var checker in referenceCheckers)
        {
            if (await checker.GetItemDeleteBlockerAsync(id, cancellationToken) is { } reason)
            {
                throw new ConflictException(reason);
            }
        }

        db.Items.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 掃到沒登錄過的條碼時，綁定到現有品項（例如同一樣東西換了包裝）。已經綁定過就直接回傳。
    /// </summary>
    public async Task<ItemDto> AddBarcodeAsync(int id, AddBarcodeRequest request, CancellationToken cancellationToken)
    {
        var item = await db.Items.Include(i => i.Barcodes).SingleOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("找不到這個品項");
        var code = NormalizeBarcode(request.Code);

        if (item.Barcodes.All(b => b.Code != code))
        {
            if (item.Barcodes.Count >= CatalogRules.MaxBarcodesPerItem)
            {
                throw new BusinessRuleException($"「{item.Name}」已經有 {CatalogRules.MaxBarcodesPerItem} 組條碼，請先移除不用的");
            }

            await EnsureBarcodesAvailableAsync([code], item.Id, cancellationToken);
            item.Barcodes.Add(new ItemBarcode { Code = code });
            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetAsync(id, cancellationToken);
    }

    internal static string NormalizeBarcode(string code) => code.Trim().ToUpperInvariant();

    private async Task EnsureBarcodesAvailableAsync(
        IReadOnlyCollection<string> codes, int itemId, CancellationToken cancellationToken)
    {
        var usedBy = await db.ItemBarcodes
            .Where(b => codes.Contains(b.Code) && b.ItemId != itemId)
            .Join(db.Items, b => b.ItemId, i => i.Id, (b, i) => new { b.Code, i.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (usedBy is not null)
        {
            throw new ConflictException($"條碼 {usedBy.Code} 已經屬於「{usedBy.Name}」");
        }
    }

    private async Task ApplyAsync(Item item, SaveItemRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await db.Items.AnyAsync(i => i.Name == name && i.Id != item.Id, cancellationToken))
        {
            throw new ConflictException($"已經有叫「{name}」的品項");
        }

        if (!await db.Units.AnyAsync(u => u.Id == request.UnitId, cancellationToken))
        {
            throw new BusinessRuleException("找不到選擇的單位");
        }

        if (request.CategoryId is { } categoryId && !await db.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            throw new BusinessRuleException("找不到選擇的分類");
        }

        if (request.LocationId is { } locationId && !await db.Locations.AnyAsync(l => l.Id == locationId, cancellationToken))
        {
            throw new BusinessRuleException("找不到選擇的位置");
        }

        var codes = (request.Barcodes ?? []).Select(NormalizeBarcode).Where(c => c.Length > 0).Distinct().ToList();
        await EnsureBarcodesAvailableAsync(codes, item.Id, cancellationToken);

        item.Name = name;
        item.CategoryId = request.CategoryId;
        item.UnitId = request.UnitId;
        item.LocationId = request.LocationId;
        item.SafetyStock = request.SafetyStock;
        item.ShelfLifeDays = request.ShelfLifeDays;
        item.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        item.IsArchived = request.IsArchived;

        item.Barcodes.RemoveAll(b => !codes.Contains(b.Code));
        foreach (var code in codes.Where(c => item.Barcodes.All(b => b.Code != c)))
        {
            item.Barcodes.Add(new ItemBarcode { Code = code });
        }
    }

    private static IQueryable<ItemDto> ProjectToDto(IQueryable<Item> items) =>
        items.Select(i => new ItemDto(
            i.Id,
            i.Name,
            i.CategoryId,
            i.Category != null ? i.Category.Name : null,
            i.UnitId,
            i.Unit.Name,
            i.LocationId,
            i.Location != null ? i.Location.Name : null,
            i.SafetyStock,
            i.ShelfLifeDays,
            i.Note,
            i.IsArchived,
            i.Barcodes.OrderBy(b => b.Id).Select(b => b.Code).ToList()));
}
