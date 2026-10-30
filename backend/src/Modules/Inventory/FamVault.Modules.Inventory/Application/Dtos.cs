using FamVault.Modules.Inventory.Domain;

namespace FamVault.Modules.Inventory.Application;

// ---- 購買 ----

public sealed record CreatePurchaseRequest(
    DateOnly PurchaseDate,
    string? Store,
    string? Note,
    IReadOnlyList<CreatePurchaseLineRequest> Lines);

/// <summary>ExpiryDate 留空時，若品項有預設保存天數就自動計算；LocationId 留空時用品項的預設位置。</summary>
public sealed record CreatePurchaseLineRequest(
    int ItemId,
    decimal Quantity,
    decimal? UnitPrice,
    DateOnly? ExpiryDate,
    int? LocationId);

public sealed record PurchaseDto(
    int Id,
    DateOnly PurchaseDate,
    string? Store,
    string? Note,
    decimal? TotalAmount,
    string? CreatedByName,
    DateTimeOffset CreatedAt,
    bool CanDelete,
    IReadOnlyList<PurchaseLineDto> Lines);

public sealed record PurchaseLineDto(
    int Id,
    int ItemId,
    string ItemName,
    string UnitName,
    decimal Quantity,
    decimal? UnitPrice,
    DateOnly? ExpiryDate,
    string? LocationName,
    int BatchId,
    decimal RemainingQuantity);

public sealed record PurchaseQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// ---- 庫存 ----

public enum StockStatusFilter
{
    All,
    InStock,
    OutOfStock,
    Low,
    Expiring,
}

public sealed record StockQuery
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public int? LocationId { get; init; }
    public StockStatusFilter Status { get; init; } = StockStatusFilter.All;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

/// <summary>「家裡還有沒有」：每個品項一列。</summary>
public sealed record StockSummaryDto(
    int ItemId,
    string Name,
    string? CategoryName,
    string UnitName,
    decimal Quantity,
    decimal ExpiredQuantity,
    int BatchCount,
    DateOnly? NearestExpiry,
    ExpiryStatus ExpiryStatus,
    decimal? SafetyStock,
    bool IsLow,
    bool IsArchived,
    IReadOnlyList<string> LocationNames);

public sealed record StockItemDetailDto(
    int ItemId,
    string Name,
    string? CategoryName,
    string UnitName,
    decimal? SafetyStock,
    bool IsLow,
    bool IsArchived,
    decimal Quantity,
    IReadOnlyList<StockBatchDto> Batches,
    IReadOnlyList<StockTransactionDto> History);

public sealed record StockBatchDto(
    int Id,
    decimal Quantity,
    decimal InitialQuantity,
    int? LocationId,
    string? LocationName,
    DateOnly PurchasedOn,
    DateOnly? ExpiryDate,
    ExpiryStatus ExpiryStatus,
    int? PurchaseId);

public sealed record StockTransactionDto(
    int Id,
    int BatchId,
    StockTransactionType Type,
    decimal QuantityChange,
    DateTimeOffset OccurredAt,
    string? UserName,
    string? Note);

public enum ConsumeKind
{
    Consume,
    Discard,
}

/// <summary>BatchId 留空時，從最早到期的批次開始扣（先到期先用）。</summary>
public sealed record ConsumeRequest(int ItemId, decimal Quantity, ConsumeKind Kind, int? BatchId, string? Note);

/// <summary>修改單一批次：盤點後更正數量、移動位置、更正到期日。</summary>
public sealed record UpdateBatchRequest(decimal Quantity, int? LocationId, DateOnly? ExpiryDate, string? Note);
