using FamVault.Modules.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Tests.Catalog;

internal static class CatalogTestDb
{
    /// <summary>建立含預設種子資料（分類 1～7、位置 1～5、單位 1～13）的記憶體資料庫。</summary>
    public static CatalogModuleDbContext Create()
    {
        var options = new DbContextOptionsBuilder<CatalogModuleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CatalogModuleDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
