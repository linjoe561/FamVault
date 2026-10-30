using FamVault.Modules.Inventory.Application;
using FamVault.Modules.Inventory.Infrastructure;
using FamVault.SharedKernel.Auth;
using FamVault.SharedKernel.Contracts;
using FamVault.SharedKernel.Persistence;
using FamVault.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Tests.Inventory;

/// <summary>
/// Inventory 服務的測試環境：記憶體資料庫，加上取代其他模組的假實作。
/// 今天固定為 2026-10-30。
/// </summary>
internal sealed class InventoryTestContext : IAsyncDisposable
{
    public static readonly DateOnly Today = new(2026, 10, 30);
    public static readonly Guid MomId = Guid.NewGuid();

    public const int Milk = 1;     // 保存 7 天、預設冰箱冷藏、安全庫存 2
    public const int Tissue = 2;   // 沒有保存期限
    public const int OldSoap = 3;  // 已停用
    public const int Fridge = 1;
    public const int Pantry = 4;

    public InventoryTestContext()
    {
        var currentUser = new FakeCurrentUser(MomId, isAdmin: false);
        Db = new InventoryModuleDbContext(new DbContextOptionsBuilder<InventoryModuleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(currentUser, TimeProvider.System))
            .Options);
        var catalog = new FakeCatalog();
        var users = new FakeUsers();
        var clock = new FixedClock();
        Purchases = new PurchaseService(Db, catalog, users, currentUser, clock);
        Stock = new StockService(Db, catalog, users, currentUser, clock);
    }

    public InventoryModuleDbContext Db { get; }
    public PurchaseService Purchases { get; }
    public StockService Stock { get; }

    public Task<PurchaseDto> BuyAsync(DateOnly date, params CreatePurchaseLineRequest[] lines) =>
        Purchases.CreateAsync(new CreatePurchaseRequest(date, "全聯", null, lines), CancellationToken.None);

    public static CreatePurchaseLineRequest Line(int itemId, decimal quantity, DateOnly? expiry = null, int? locationId = null) =>
        new(itemId, quantity, UnitPrice: 50, expiry, locationId);

    public ValueTask DisposeAsync() => Db.DisposeAsync();

    private sealed class FakeCatalog : ICatalogLookup
    {
        private static readonly CatalogItemInfo[] Items =
        [
            new(Milk, "鮮奶", 2, "飲料", "瓶", Fridge, SafetyStock: 2, ShelfLifeDays: 7, IsArchived: false),
            new(Tissue, "衛生紙", 4, "日用品", "包", Pantry, SafetyStock: null, ShelfLifeDays: null, IsArchived: false),
            new(OldSoap, "舊肥皂", 6, "個人保養", "個", null, SafetyStock: null, ShelfLifeDays: null, IsArchived: true),
        ];

        public Task<IReadOnlyList<CatalogItemInfo>> FindItemsAsync(
            CatalogItemFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogItemInfo>>(Items
                .Where(i => filter.IncludeArchived || !i.IsArchived)
                .Where(i => filter.Ids is null || filter.Ids.Contains(i.Id))
                .Where(i => filter.Search is null || i.Name.Contains(filter.Search))
                .Where(i => filter.CategoryId is null || i.CategoryId == filter.CategoryId)
                .ToList());

        public Task<IReadOnlyDictionary<int, string>> GetLocationNamesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(
                new Dictionary<int, string> { [Fridge] = "冰箱冷藏", [Pantry] = "儲藏室" });
    }

    private sealed class FakeUsers : IUserDirectory
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IEnumerable<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                userIds.Distinct().Where(id => id == MomId).ToDictionary(id => id, _ => "媽媽"));
    }

    private sealed class FakeCurrentUser(Guid userId, bool isAdmin) : ICurrentUser
    {
        public Guid? UserId => userId;
        public string? DisplayName => "媽媽";
        public bool IsInRole(string role) => isAdmin && role == AppRoles.Admin;
    }

    private sealed class FixedClock : IAppClock
    {
        public DateTimeOffset Now => new(2026, 10, 30, 10, 0, 0, TimeSpan.FromHours(8));
        public DateOnly Today => InventoryTestContext.Today;
    }
}
