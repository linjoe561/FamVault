using FamVault.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Catalog.Infrastructure;

public sealed class CatalogModuleDbContext(DbContextOptions<CatalogModuleDbContext> options) : DbContext(options)
{
    public const string Schema = "catalog";

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemBarcode> ItemBarcodes => Set<ItemBarcode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<Category>(category =>
        {
            category.Property(c => c.Name).HasMaxLength(50).IsRequired();
            category.HasOne<Category>().WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
            category.HasIndex(c => new { c.ParentId, c.Name });
        });

        builder.Entity<Location>(location =>
        {
            location.Property(l => l.Name).HasMaxLength(50).IsRequired();
            location.Property(l => l.Description).HasMaxLength(200);
            location.HasIndex(l => l.Name).IsUnique();
        });

        builder.Entity<Unit>(unit =>
        {
            unit.Property(u => u.Name).HasMaxLength(10).IsRequired();
            unit.HasIndex(u => u.Name).IsUnique();
        });

        builder.Entity<Item>(item =>
        {
            item.Property(i => i.Name).HasMaxLength(100).IsRequired();
            item.Property(i => i.Note).HasMaxLength(500);
            item.Property(i => i.SafetyStock).HasPrecision(12, 3);
            item.HasIndex(i => i.Name).IsUnique();
            item.HasOne(i => i.Category).WithMany().HasForeignKey(i => i.CategoryId).OnDelete(DeleteBehavior.Restrict);
            item.HasOne(i => i.Unit).WithMany().HasForeignKey(i => i.UnitId).OnDelete(DeleteBehavior.Restrict);
            item.HasOne(i => i.Location).WithMany().HasForeignKey(i => i.LocationId).OnDelete(DeleteBehavior.Restrict);
            item.HasMany(i => i.Barcodes).WithOne().HasForeignKey(b => b.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ItemBarcode>(barcode =>
        {
            barcode.Property(b => b.Code).HasMaxLength(50).IsRequired();
            barcode.HasIndex(b => b.Code).IsUnique();
        });

        CatalogSeedData.Apply(builder);
    }
}
