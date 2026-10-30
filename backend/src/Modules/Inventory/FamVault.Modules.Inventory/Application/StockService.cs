using System.Globalization;
using FamVault.Modules.Inventory.Domain;
using FamVault.Modules.Inventory.Infrastructure;
using FamVault.SharedKernel.Auth;
using FamVault.SharedKernel.Contracts;
using FamVault.SharedKernel.Errors;
using FamVault.SharedKernel.Time;
using FamVault.SharedKernel.Web;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Inventory.Application;

/// <summary>「家裡還有沒有」的查詢，以及用掉、丟棄、盤點調整。</summary>
public sealed class StockService(
    InventoryModuleDbContext db,
    ICatalogLookup catalog,
    IUserDirectory users,
    ICurrentUser currentUser,
    IAppClock clock)
{
    private const int HistoryLimit = 30;

    /// <summary>
    /// 每個品項一列。沒有庫存的品項也會列出（數量 0），才能回答「家裡沒有了」；
    /// 已停用的品項只有在還有庫存時才列出。家用資料量小，彙總在記憶體中完成。
    /// </summary>
    public async Task<PagedResult<StockSummaryDto>> GetOverviewAsync(StockQuery query, CancellationToken cancellationToken)
    {
        var today = clock.Today;
        var batches = await db.StockBatches.AsNoTracking()
            .Where(b => b.RemainingQuantity > 0)
            .Select(b => new BatchSlice(b.ItemId, b.RemainingQuantity, b.ExpiryDate, b.LocationId))
            .ToListAsync(cancellationToken);
        var batchesByItem = batches.ToLookup(b => b.ItemId);

        var items = await catalog.FindItemsAsync(
            new CatalogItemFilter(query.Search, query.CategoryId, IncludeArchived: true), cancellationToken);
        var locations = await catalog.GetLocationNamesAsync(cancellationToken);

        var rows = items
            .Select(item => (Item: item, Batches: batchesByItem[item.Id].ToList()))
            .Where(x => !x.Item.IsArchived || x.Batches.Count > 0)
            .Where(x => query.LocationId is not { } locationId || x.Batches.Any(b => b.LocationId == locationId))
            .Select(x => Summarize(x.Item, x.Batches, locations, today))
            .Where(row => query.Status switch
            {
                StockStatusFilter.InStock => row.Quantity > 0,
                StockStatusFilter.OutOfStock => row.Quantity == 0,
                StockStatusFilter.Low => row.IsLow,
                StockStatusFilter.Expiring => row.ExpiryStatus is ExpiryStatus.Expired or ExpiryStatus.Soon,
                _ => true,
            })
            .ToList();

        if (query.Status == StockStatusFilter.Expiring)
        {
            rows = rows.OrderBy(r => r.NearestExpiry).ThenBy(r => r.Name).ToList();
        }

        var page = rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();
        return new PagedResult<StockSummaryDto>(page, rows.Count, query.Page, query.PageSize);
    }

    public async Task<StockItemDetailDto> GetItemAsync(int itemId, CancellationToken cancellationToken)
    {
        var item = await FindItemAsync(itemId, cancellationToken);
        var today = clock.Today;
        var locations = await catalog.GetLocationNamesAsync(cancellationToken);

        var batches = await OrderForUse(db.StockBatches.AsNoTracking()
                .Where(b => b.ItemId == itemId && b.RemainingQuantity > 0))
            .Select(b => new
            {
                b.Id,
                b.RemainingQuantity,
                b.InitialQuantity,
                b.LocationId,
                b.PurchasedOn,
                b.ExpiryDate,
                PurchaseId = b.PurchaseLine != null ? (int?)b.PurchaseLine.PurchaseId : null,
            })
            .ToListAsync(cancellationToken);

        var history = await db.StockTransactions.AsNoTracking()
            .Where(t => t.ItemId == itemId)
            .OrderByDescending(t => t.OccurredAt).ThenByDescending(t => t.Id)
            .Take(HistoryLimit)
            .ToListAsync(cancellationToken);
        var userNames = await users.GetDisplayNamesAsync(
            history.Where(t => t.UserId is not null).Select(t => t.UserId!.Value), cancellationToken);

        var quantity = batches.Sum(b => b.RemainingQuantity);
        var expired = batches.Where(b => b.ExpiryDate < today).Sum(b => b.RemainingQuantity);

        return new StockItemDetailDto(
            item.Id,
            item.Name,
            item.CategoryName,
            item.UnitName,
            item.SafetyStock,
            IsLow(item.SafetyStock, quantity - expired),
            item.IsArchived,
            quantity,
            batches.Select(b => new StockBatchDto(
                b.Id,
                b.RemainingQuantity,
                b.InitialQuantity,
                b.LocationId,
                b.LocationId is { } locationId ? locations.GetValueOrDefault(locationId) : null,
                b.PurchasedOn,
                b.ExpiryDate,
                ExpiryRules.GetStatus(b.ExpiryDate, today),
                b.PurchaseId)).ToList(),
            history.Select(t => new StockTransactionDto(
                t.Id,
                t.StockBatchId,
                t.Type,
                t.QuantityChange,
                t.OccurredAt,
                t.UserId is { } userId ? userNames.GetValueOrDefault(userId) : null,
                t.Note)).ToList());
    }

    /// <summary>
    /// 用掉或丟棄。沒指定批次時，依「先到期先用」從最早到期的批次開始扣，一批不夠就扣下一批。
    /// </summary>
    public async Task<StockItemDetailDto> ConsumeAsync(ConsumeRequest request, CancellationToken cancellationToken)
    {
        var item = await FindItemAsync(request.ItemId, cancellationToken);

        var candidates = db.StockBatches.Where(b => b.ItemId == request.ItemId && b.RemainingQuantity > 0);
        if (request.BatchId is { } batchId)
        {
            candidates = candidates.Where(b => b.Id == batchId);
        }

        var batches = await OrderForUse(candidates).ToListAsync(cancellationToken);
        var available = batches.Sum(b => b.RemainingQuantity);
        if (available < request.Quantity)
        {
            throw new BusinessRuleException(available == 0
                ? $"「{item.Name}」{(request.BatchId is null ? "目前沒有庫存" : "這一批已經用完了")}"
                : $"庫存不足：{(request.BatchId is null ? "目前" : "這一批")}只剩 {FormatQuantity(available)} {item.UnitName}");
        }

        var type = request.Kind == ConsumeKind.Discard ? StockTransactionType.Discard : StockTransactionType.Consume;
        var left = request.Quantity;
        foreach (var batch in batches)
        {
            var take = Math.Min(batch.RemainingQuantity, left);
            batch.Record(type, -take, clock.Now, currentUser.UserId, request.Note);
            left -= take;
            if (left == 0)
            {
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetItemAsync(request.ItemId, cancellationToken);
    }

    /// <summary>盤點後更正數量、移到別的位置、更正到期日。數量有變化時會留下「調整」紀錄。</summary>
    public async Task<StockItemDetailDto> UpdateBatchAsync(
        int batchId, UpdateBatchRequest request, CancellationToken cancellationToken)
    {
        var batch = await db.StockBatches.FindAsync([batchId], cancellationToken)
            ?? throw new NotFoundException("找不到這一批庫存");

        if (request.LocationId is { } locationId
            && !(await catalog.GetLocationNamesAsync(cancellationToken)).ContainsKey(locationId))
        {
            throw new BusinessRuleException("找不到選擇的存放位置");
        }

        batch.LocationId = request.LocationId;
        batch.ExpiryDate = request.ExpiryDate;
        var change = request.Quantity - batch.RemainingQuantity;
        if (change != 0)
        {
            batch.Record(StockTransactionType.Adjust, change, clock.Now, currentUser.UserId, request.Note);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetItemAsync(batch.ItemId, cancellationToken);
    }

    internal static string FormatQuantity(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>先到期先用：有到期日的依日期排前面，沒有到期日的排最後，再依購買日。</summary>
    private static IQueryable<StockBatch> OrderForUse(IQueryable<StockBatch> batches) =>
        batches
            .OrderBy(b => b.ExpiryDate == null)
            .ThenBy(b => b.ExpiryDate)
            .ThenBy(b => b.PurchasedOn)
            .ThenBy(b => b.Id);

    /// <summary>低於安全庫存：只算還沒過期的數量。</summary>
    private static bool IsLow(decimal? safetyStock, decimal usableQuantity) =>
        safetyStock is { } safety && usableQuantity < safety;

    private async Task<CatalogItemInfo> FindItemAsync(int itemId, CancellationToken cancellationToken) =>
        (await catalog.FindItemsAsync(new CatalogItemFilter(Ids: [itemId], IncludeArchived: true), cancellationToken))
        .SingleOrDefault()
        ?? throw new NotFoundException("找不到這個品項");

    private static StockSummaryDto Summarize(
        CatalogItemInfo item, IReadOnlyList<BatchSlice> batches, IReadOnlyDictionary<int, string> locations, DateOnly today)
    {
        var quantity = batches.Sum(b => b.Quantity);
        var expired = batches.Where(b => b.ExpiryDate < today).Sum(b => b.Quantity);
        var nearestExpiry = batches.Where(b => b.ExpiryDate is not null).Min(b => b.ExpiryDate);

        return new StockSummaryDto(
            item.Id,
            item.Name,
            item.CategoryName,
            item.UnitName,
            quantity,
            expired,
            batches.Count,
            nearestExpiry,
            ExpiryRules.GetStatus(nearestExpiry, today),
            item.SafetyStock,
            IsLow(item.SafetyStock, quantity - expired),
            item.IsArchived,
            batches
                .Where(b => b.LocationId is not null)
                .Select(b => locations.GetValueOrDefault(b.LocationId!.Value))
                .OfType<string>()
                .Distinct()
                .ToList());
    }

    private sealed record BatchSlice(int ItemId, decimal Quantity, DateOnly? ExpiryDate, int? LocationId);
}
