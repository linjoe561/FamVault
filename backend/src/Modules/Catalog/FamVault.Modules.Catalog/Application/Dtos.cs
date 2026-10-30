namespace FamVault.Modules.Catalog.Application;

public sealed record CategoryDto(int Id, string Name, int? ParentId, int ItemCount);

public sealed record SaveCategoryRequest(string Name, int? ParentId);

public sealed record LocationDto(int Id, string Name, string? Description, int ItemCount);

public sealed record SaveLocationRequest(string Name, string? Description);

public sealed record UnitDto(int Id, string Name, int ItemCount);

public sealed record SaveUnitRequest(string Name);

public sealed record ItemDto(
    int Id,
    string Name,
    int? CategoryId,
    string? CategoryName,
    int UnitId,
    string UnitName,
    int? LocationId,
    string? LocationName,
    decimal? SafetyStock,
    int? ShelfLifeDays,
    string? Note,
    bool IsArchived,
    IReadOnlyList<string> Barcodes);

public sealed record SaveItemRequest(
    string Name,
    int? CategoryId,
    int UnitId,
    int? LocationId,
    decimal? SafetyStock,
    int? ShelfLifeDays,
    string? Note,
    IReadOnlyList<string>? Barcodes,
    bool IsArchived);

public sealed record AddBarcodeRequest(string Code);

/// <summary>品項清單的查詢條件。Search 會比對名稱（部分符合）與條碼（完全符合）。</summary>
public sealed record ItemQuery
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public int? LocationId { get; init; }
    public bool IncludeArchived { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
