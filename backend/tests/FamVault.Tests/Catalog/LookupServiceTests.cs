using FamVault.Modules.Catalog.Application;
using FamVault.SharedKernel.Errors;

namespace FamVault.Tests.Catalog;

public class LookupServiceTests
{
    [Fact]
    public async Task Categories_are_limited_to_two_levels()
    {
        await using var db = CatalogTestDb.Create();
        var service = new CategoryService(db);
        var dairy = await service.CreateAsync(new SaveCategoryRequest("乳製品", ParentId: 1), CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(new SaveCategoryRequest("優格", dairy.Id), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateAsync(1, new SaveCategoryRequest("食品", ParentId: 2), CancellationToken.None));
    }

    [Fact]
    public async Task Category_in_use_cannot_be_deleted()
    {
        await using var db = CatalogTestDb.Create();
        await new ItemService(db).CreateAsync(
            new SaveItemRequest("白米", 1, 10, null, null, null, null, null, false), CancellationToken.None);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            new CategoryService(db).DeleteAsync(1, CancellationToken.None));

        Assert.Contains("1 個品項", error.Message);
    }

    [Fact]
    public async Task Unused_unit_can_be_deleted_and_duplicate_name_is_rejected()
    {
        await using var db = CatalogTestDb.Create();
        var service = new UnitService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new SaveUnitRequest("瓶"), CancellationToken.None));

        await service.DeleteAsync(13, CancellationToken.None);
        Assert.DoesNotContain(await service.ListAsync(CancellationToken.None), u => u.Name == "毫升");
    }
}
