using FamVault.Modules.Inventory.Domain;
using FamVault.Modules.Inventory.Infrastructure;
using FamVault.SharedKernel.Auth;
using FamVault.SharedKernel.Contracts;
using FamVault.SharedKernel.Errors;
using FamVault.SharedKernel.Time;
using FamVault.SharedKernel.Web;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Inventory.Application;

/// <summary>購買登錄：一次採買建立多個庫存批次。</summary>
public sealed class PurchaseService(
    InventoryModuleDbContext db,
    ICatalogLookup catalog,
    IUserDirectory users,
    ICurrentUser currentUser,
    IAppClock clock)
{
    public async Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, CancellationToken cancellationToken)
    {
        if (request.PurchaseDate > clock.Today)
        {
            throw new BusinessRuleException("購買日期不能是未來的日子");
        }

        var items = (await catalog.FindItemsAsync(
                new CatalogItemFilter(Ids: request.Lines.Select(l => l.ItemId).Distinct().ToList(), IncludeArchived: true),
                cancellationToken))
            .ToDictionary(i => i.Id);
        var locations = await catalog.GetLocationNamesAsync(cancellationToken);

        var purchase = new Purchase
        {
            PurchaseDate = request.PurchaseDate,
            Store = NullIfBlank(request.Store),
            Note = NullIfBlank(request.Note),
        };

        foreach (var line in request.Lines)
        {
            if (!items.TryGetValue(line.ItemId, out var item))
            {
                throw new BusinessRuleException("找不到選擇的品項，可能已被刪除");
            }

            if (item.IsArchived)
            {
                throw new BusinessRuleException($"「{item.Name}」已停用，請先到品項中取消停用");
            }

            if (line.LocationId is { } locationId && !locations.ContainsKey(locationId))
            {
                throw new BusinessRuleException("找不到選擇的存放位置");
            }

            var batch = new StockBatch
            {
                ItemId = item.Id,
                LocationId = line.LocationId ?? item.LocationId,
                PurchasedOn = request.PurchaseDate,
                ExpiryDate = line.ExpiryDate ?? DefaultExpiry(request.PurchaseDate, item.ShelfLifeDays),
                InitialQuantity = line.Quantity,
            };
            batch.Record(StockTransactionType.Purchase, line.Quantity, clock.Now, currentUser.UserId, note: null);

            purchase.Lines.Add(new PurchaseLine
            {
                ItemId = item.Id,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Batch = batch,
            });
        }

        db.Purchases.Add(purchase);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(purchase.Id, cancellationToken);
    }

    public async Task<PagedResult<PurchaseDto>> ListAsync(PurchaseQuery query, CancellationToken cancellationToken)
    {
        var purchases = db.Purchases.AsNoTracking();
        var total = await purchases.CountAsync(cancellationToken);
        var page = await purchases
            .Include(p => p.Lines).ThenInclude(l => l.Batch)
            .OrderByDescending(p => p.PurchaseDate).ThenByDescending(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseDto>(await ToDtosAsync(page, cancellationToken), total, query.Page, query.PageSize);
    }

    public async Task<PurchaseDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var purchase = await LoadAsync(id, cancellationToken, tracking: false);
        return (await ToDtosAsync([purchase], cancellationToken))[0];
    }

    /// <summary>
    /// 刪除整筆購買（例如登錄錯了）。只有還沒被用掉或調整過時才能刪，
    /// 而且只有登錄的人或管理者可以刪。
    /// </summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var purchase = await LoadAsync(id, cancellationToken, tracking: true);
        if (!CanManage(purchase))
        {
            throw new ForbiddenException("只有登錄這筆購買的人或管理者可以刪除");
        }

        var touched = await TouchedBatchIdsAsync(purchase.Lines.Select(l => l.Batch.Id), cancellationToken);
        if (touched.Count > 0)
        {
            throw new ConflictException("這次購買的東西已經有用掉或調整過，不能整筆刪除；請到庫存中調整數量");
        }

        db.Purchases.Remove(purchase);
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static DateOnly? DefaultExpiry(DateOnly purchaseDate, int? shelfLifeDays) =>
        shelfLifeDays is { } days ? purchaseDate.AddDays(days) : null;

    private async Task<Purchase> LoadAsync(int id, CancellationToken cancellationToken, bool tracking)
    {
        var query = tracking ? db.Purchases : db.Purchases.AsNoTracking();
        return await query
            .Include(p => p.Lines).ThenInclude(l => l.Batch)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException("找不到這筆購買紀錄");
    }

    private bool CanManage(Purchase purchase) =>
        currentUser.IsInRole(AppRoles.Admin) || (purchase.CreatedBy is { } createdBy && createdBy == currentUser.UserId);

    /// <summary>除了購買入庫之外，還有其他異動（用掉、丟棄、調整）的批次。</summary>
    private async Task<HashSet<int>> TouchedBatchIdsAsync(IEnumerable<int> batchIds, CancellationToken cancellationToken)
    {
        var ids = batchIds.ToList();
        return (await db.StockTransactions
                .Where(t => ids.Contains(t.StockBatchId) && t.Type != StockTransactionType.Purchase)
                .Select(t => t.StockBatchId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
    }

    private async Task<IReadOnlyList<PurchaseDto>> ToDtosAsync(
        IReadOnlyList<Purchase> purchases, CancellationToken cancellationToken)
    {
        var lines = purchases.SelectMany(p => p.Lines).ToList();
        var items = (await catalog.FindItemsAsync(
                new CatalogItemFilter(Ids: lines.Select(l => l.ItemId).Distinct().ToList(), IncludeArchived: true),
                cancellationToken))
            .ToDictionary(i => i.Id);
        var locations = await catalog.GetLocationNamesAsync(cancellationToken);
        var userNames = await users.GetDisplayNamesAsync(
            purchases.Where(p => p.CreatedBy is not null).Select(p => p.CreatedBy!.Value), cancellationToken);
        var touched = await TouchedBatchIdsAsync(lines.Select(l => l.Batch.Id), cancellationToken);

        return purchases.Select(p =>
        {
            var priced = p.Lines.Where(l => l.UnitPrice is not null).ToList();
            return new PurchaseDto(
                p.Id,
                p.PurchaseDate,
                p.Store,
                p.Note,
                priced.Count > 0 ? priced.Sum(l => l.Quantity * l.UnitPrice!.Value) : null,
                p.CreatedBy is { } userId ? userNames.GetValueOrDefault(userId) : null,
                p.CreatedAt,
                CanManage(p) && p.Lines.All(l => !touched.Contains(l.Batch.Id)),
                p.Lines.OrderBy(l => l.Id).Select(l => new PurchaseLineDto(
                    l.Id,
                    l.ItemId,
                    items.GetValueOrDefault(l.ItemId)?.Name ?? "（已刪除的品項）",
                    items.GetValueOrDefault(l.ItemId)?.UnitName ?? string.Empty,
                    l.Quantity,
                    l.UnitPrice,
                    l.Batch.ExpiryDate,
                    l.Batch.LocationId is { } locationId ? locations.GetValueOrDefault(locationId) : null,
                    l.Batch.Id,
                    l.Batch.RemainingQuantity)).ToList());
        }).ToList();
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
