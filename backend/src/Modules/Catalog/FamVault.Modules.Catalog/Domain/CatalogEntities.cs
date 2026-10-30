using FamVault.SharedKernel.Domain;

namespace FamVault.Modules.Catalog.Domain;

/// <summary>分類，最多兩層（大分類 → 小分類）。</summary>
public sealed class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>存放位置，例如冰箱冷藏、儲藏室。</summary>
public sealed class Location : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>計量單位，例如瓶、包、公斤。</summary>
public sealed class Unit : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>
/// 品項主檔：「這是什麼東西」。實際數量與保存期限記在之後的庫存批次上，不在這裡。
/// </summary>
public sealed class Item : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    /// <summary>預設存放位置，購買登錄時帶入。</summary>
    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    /// <summary>安全庫存量：低於這個數量時提醒補貨。</summary>
    public decimal? SafetyStock { get; set; }

    /// <summary>預設保存天數：購買登錄時用來自動算出到期日。</summary>
    public int? ShelfLifeDays { get; set; }

    public string? Note { get; set; }

    /// <summary>停用的品項不會出現在一般清單與購買登錄中，但資料保留。</summary>
    public bool IsArchived { get; set; }

    public List<ItemBarcode> Barcodes { get; set; } = [];
}

/// <summary>同一個品項可能有多種條碼（不同包裝或品牌）。條碼在全系統唯一。</summary>
public sealed class ItemBarcode
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string Code { get; set; } = string.Empty;
}
