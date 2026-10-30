using FamVault.SharedKernel.Domain;

namespace FamVault.Modules.Inventory.Domain;

/// <summary>一次採買。每一行明細會建立一個庫存批次。</summary>
public sealed class Purchase : AuditableEntity
{
    public DateOnly PurchaseDate { get; set; }
    public string? Store { get; set; }
    public string? Note { get; set; }
    public List<PurchaseLine> Lines { get; set; } = [];
}

public sealed class PurchaseLine
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }

    /// <summary>Catalog 模組的品項 Id（跨模組，不建外鍵）。</summary>
    public int ItemId { get; set; }

    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public StockBatch Batch { get; set; } = null!;
}

/// <summary>
/// 庫存批次：同一樣東西每次買的都分開記，因為到期日不同。
/// 「家裡還有多少」= 該品項所有批次的 RemainingQuantity 加總。
/// </summary>
public sealed class StockBatch : AuditableEntity
{
    public int ItemId { get; set; }

    /// <summary>Catalog 模組的位置 Id（跨模組，不建外鍵）。</summary>
    public int? LocationId { get; set; }

    public int? PurchaseLineId { get; set; }
    public PurchaseLine? PurchaseLine { get; set; }

    public DateOnly PurchasedOn { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }

    /// <summary>樂觀鎖：兩位家人同時扣同一批時，後送出的會得到 409。</summary>
    public byte[] RowVersion { get; set; } = [];

    public List<StockTransaction> Transactions { get; set; } = [];

    /// <summary>記錄一筆異動並同步調整剩餘數量。所有數量變化都要經過這裡，紀錄才不會漏。</summary>
    public void Record(StockTransactionType type, decimal quantityChange, DateTimeOffset occurredAt, Guid? userId, string? note)
    {
        RemainingQuantity += quantityChange;
        Transactions.Add(new StockTransaction
        {
            ItemId = ItemId,
            Type = type,
            QuantityChange = quantityChange,
            OccurredAt = occurredAt,
            UserId = userId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        });
    }
}

public enum StockTransactionType
{
    /// <summary>購買入庫</summary>
    Purchase,

    /// <summary>用掉</summary>
    Consume,

    /// <summary>丟棄（過期、壞掉）</summary>
    Discard,

    /// <summary>盤點調整（數量可正可負）</summary>
    Adjust,
}

/// <summary>庫存異動紀錄：誰、什麼時候、對哪一批做了什麼。只新增不修改。</summary>
public sealed class StockTransaction
{
    public int Id { get; set; }
    public int StockBatchId { get; set; }
    public int ItemId { get; set; }
    public StockTransactionType Type { get; set; }

    /// <summary>數量變化：入庫為正，用掉與丟棄為負，調整可正可負。</summary>
    public decimal QuantityChange { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
    public Guid? UserId { get; set; }
    public string? Note { get; set; }
}
