namespace FamVault.SharedKernel.Contracts;

/// <summary>其他模組看到的品項資料（由 Catalog 模組提供）。</summary>
public sealed record CatalogItemInfo(
    int Id,
    string Name,
    int? CategoryId,
    string? CategoryName,
    string UnitName,
    int? LocationId,
    decimal? SafetyStock,
    int? ShelfLifeDays,
    bool IsArchived);

/// <summary>
/// 查詢條件。Search 比對名稱（部分符合）或條碼（完全符合）；CategoryId 為大分類時包含底下小分類。
/// </summary>
public sealed record CatalogItemFilter(
    string? Search = null,
    int? CategoryId = null,
    IReadOnlyCollection<int>? Ids = null,
    bool IncludeArchived = false);

/// <summary>跨模組讀取品項與存放位置。由 Catalog 模組實作。</summary>
public interface ICatalogLookup
{
    Task<IReadOnlyList<CatalogItemInfo>> FindItemsAsync(
        CatalogItemFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, string>> GetLocationNamesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 其他模組回報「還有資料在使用某個品項或位置」，Catalog 刪除前會詢問所有實作。
/// 例如 Inventory 模組：有庫存紀錄的品項不能刪除、還有庫存放著的位置不能刪除。
/// </summary>
public interface ICatalogReferenceChecker
{
    /// <summary>回傳阻止刪除的原因；可以刪除時回傳 null。</summary>
    Task<string?> GetItemDeleteBlockerAsync(int itemId, CancellationToken cancellationToken = default);

    /// <summary>回傳阻止刪除的原因；可以刪除時回傳 null。</summary>
    Task<string?> GetLocationDeleteBlockerAsync(int locationId, CancellationToken cancellationToken = default);
}
