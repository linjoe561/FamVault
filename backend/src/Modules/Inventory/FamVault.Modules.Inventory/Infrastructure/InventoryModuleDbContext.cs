using FamVault.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Inventory.Infrastructure;

public sealed class InventoryModuleDbContext(DbContextOptions<InventoryModuleDbContext> options) : DbContext(options)
{
    public const string Schema = "inventory";

    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseLine> PurchaseLines => Set<PurchaseLine>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<Purchase>(purchase =>
        {
            purchase.Property(p => p.Store).HasMaxLength(50);
            purchase.Property(p => p.Note).HasMaxLength(200);
            purchase.HasIndex(p => p.PurchaseDate);
            purchase.HasMany(p => p.Lines).WithOne().HasForeignKey(l => l.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PurchaseLine>(line =>
        {
            line.Property(l => l.Quantity).HasPrecision(12, 3);
            line.Property(l => l.UnitPrice).HasPrecision(12, 2);
            line.HasOne(l => l.Batch).WithOne(b => b.PurchaseLine)
                .HasForeignKey<StockBatch>(b => b.PurchaseLineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StockBatch>(batch =>
        {
            batch.Property(b => b.InitialQuantity).HasPrecision(12, 3);
            batch.Property(b => b.RemainingQuantity).HasPrecision(12, 3);
            batch.Property(b => b.RowVersion).IsRowVersion();
            batch.HasIndex(b => new { b.ItemId, b.RemainingQuantity });
            batch.HasIndex(b => b.ExpiryDate);
        });

        builder.Entity<StockTransaction>(transaction =>
        {
            transaction.Property(t => t.QuantityChange).HasPrecision(12, 3);
            transaction.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            transaction.Property(t => t.Note).HasMaxLength(200);
            transaction.HasIndex(t => new { t.ItemId, t.OccurredAt });
            transaction.HasOne<StockBatch>().WithMany(b => b.Transactions)
                .HasForeignKey(t => t.StockBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
