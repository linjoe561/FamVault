using FamVault.Modules.Catalog.Application;
using FamVault.SharedKernel.Errors;

namespace FamVault.Tests.Catalog;

public class ItemServiceTests
{
    private const int UnitBottle = 2;
    private const int CategoryFood = 1;

    private static SaveItemRequest Milk(params string[] barcodes) =>
        new("鮮奶", CategoryFood, UnitBottle, LocationId: 1, SafetyStock: 1, ShelfLifeDays: 7, Note: null, barcodes, IsArchived: false);

    [Fact]
    public async Task Create_normalizes_and_deduplicates_barcodes()
    {
        await using var db = CatalogTestDb.Create();
        var service = new ItemService(db);

        var item = await service.CreateAsync(Milk(" 4710088abc ", "4710088ABC", "123"), CancellationToken.None);

        Assert.Equal(["4710088ABC", "123"], item.Barcodes);
        Assert.Equal("瓶", item.UnitName);
        Assert.Equal("食品", item.CategoryName);
    }

    [Fact]
    public async Task Barcode_already_used_by_another_item_is_rejected()
    {
        await using var db = CatalogTestDb.Create();
        var service = new ItemService(db);
        await service.CreateAsync(Milk("4710088"), CancellationToken.None);

        var other = Milk("4710088") with { Name = "豆漿" };
        var error = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(other, CancellationToken.None));

        Assert.Contains("鮮奶", error.Message);
    }

    [Fact]
    public async Task Duplicate_name_is_rejected()
    {
        await using var db = CatalogTestDb.Create();
        var service = new ItemService(db);
        await service.CreateAsync(Milk(), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(Milk(), CancellationToken.None));
    }

    [Fact]
    public async Task Update_replaces_barcodes_and_keeps_existing_ones()
    {
        await using var db = CatalogTestDb.Create();
        var service = new ItemService(db);
        var created = await service.CreateAsync(Milk("111", "222"), CancellationToken.None);

        var updated = await service.UpdateAsync(created.Id, Milk("222", "333"), CancellationToken.None);

        Assert.Equal(["222", "333"], updated.Barcodes);
        Assert.Equal(created.Id, (await service.GetByBarcodeAsync("333", CancellationToken.None)).Id);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByBarcodeAsync("111", CancellationToken.None));
    }

    [Fact]
    public async Task List_hides_archived_items_and_filters_by_parent_category()
    {
        await using var db = CatalogTestDb.Create();
        var categories = new CategoryService(db);
        var dairy = await categories.CreateAsync(new SaveCategoryRequest("乳製品", CategoryFood), CancellationToken.None);
        var items = new ItemService(db);
        await items.CreateAsync(Milk() with { CategoryId = dairy.Id }, CancellationToken.None);
        await items.CreateAsync(Milk() with { Name = "舊牌子鮮奶", IsArchived = true }, CancellationToken.None);

        var result = await items.ListAsync(new ItemQuery { CategoryId = CategoryFood }, CancellationToken.None);

        Assert.Equal(1, result.Total);
        Assert.Equal("鮮奶", result.Items[0].Name);
    }
}
