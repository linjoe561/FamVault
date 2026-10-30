using FamVault.Modules.Inventory.Application;
using FamVault.Modules.Inventory.Domain;
using FamVault.SharedKernel.Errors;
using static FamVault.Tests.Inventory.InventoryTestContext;

namespace FamVault.Tests.Inventory;

public class InventoryServiceTests
{
    [Fact]
    public async Task Purchase_uses_item_defaults_for_expiry_and_location()
    {
        await using var ctx = new InventoryTestContext();

        var purchase = await ctx.BuyAsync(Today.AddDays(-1), Line(Milk, 2), Line(Tissue, 3, locationId: Fridge));

        Assert.Equal(Today.AddDays(6), purchase.Lines[0].ExpiryDate); // 前一天買 + 保存 7 天
        Assert.Equal("冰箱冷藏", purchase.Lines[0].LocationName);
        Assert.Null(purchase.Lines[1].ExpiryDate);
        Assert.Equal("冰箱冷藏", purchase.Lines[1].LocationName);       // 有指定就不用預設
        Assert.Equal(250m, purchase.TotalAmount);                        // (2 + 3) × 50
        Assert.Equal("媽媽", purchase.CreatedByName);
    }

    [Fact]
    public async Task Purchase_rejects_future_date_and_archived_items()
    {
        await using var ctx = new InventoryTestContext();

        await Assert.ThrowsAsync<BusinessRuleException>(() => ctx.BuyAsync(Today.AddDays(1), Line(Milk, 1)));
        await Assert.ThrowsAsync<BusinessRuleException>(() => ctx.BuyAsync(Today, Line(OldSoap, 1)));
    }

    [Fact]
    public async Task Consume_takes_from_earliest_expiry_first_across_batches()
    {
        await using var ctx = new InventoryTestContext();
        await ctx.BuyAsync(Today, Line(Milk, 2, expiry: Today.AddDays(10)));
        await ctx.BuyAsync(Today, Line(Milk, 1, expiry: Today.AddDays(2)));

        var detail = await ctx.Stock.ConsumeAsync(
            new ConsumeRequest(Milk, 2, ConsumeKind.Consume, BatchId: null, Note: null), CancellationToken.None);

        var batch = Assert.Single(detail.Batches);          // 先到期的那批用完了
        Assert.Equal(Today.AddDays(10), batch.ExpiryDate);
        Assert.Equal(1m, batch.Quantity);
        Assert.Equal(1m, detail.Quantity);
        Assert.Equal(2, detail.History.Count(t => t.Type == StockTransactionType.Consume));
        Assert.All(detail.History, t => Assert.Equal("媽媽", t.UserName));
    }

    [Fact]
    public async Task Consume_more_than_available_is_rejected_without_changes()
    {
        await using var ctx = new InventoryTestContext();
        await ctx.BuyAsync(Today, Line(Milk, 1));

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => ctx.Stock.ConsumeAsync(
            new ConsumeRequest(Milk, 1.5m, ConsumeKind.Discard, null, null), CancellationToken.None));

        Assert.Contains("只剩 1 瓶", error.Message);
        Assert.Equal(1m, (await ctx.Stock.GetItemAsync(Milk, CancellationToken.None)).Quantity);
    }

    [Fact]
    public async Task Purchase_can_be_deleted_only_before_anything_is_used()
    {
        await using var ctx = new InventoryTestContext();
        var untouched = await ctx.BuyAsync(Today, Line(Tissue, 3));
        var used = await ctx.BuyAsync(Today, Line(Milk, 2));
        Assert.True(untouched.CanDelete);

        await ctx.Stock.ConsumeAsync(new ConsumeRequest(Milk, 1, ConsumeKind.Consume, null, null), CancellationToken.None);

        Assert.False((await ctx.Purchases.GetAsync(used.Id, CancellationToken.None)).CanDelete);
        await Assert.ThrowsAsync<ConflictException>(() => ctx.Purchases.DeleteAsync(used.Id, CancellationToken.None));
        await ctx.Purchases.DeleteAsync(untouched.Id, CancellationToken.None);
        Assert.Equal(0m, (await ctx.Stock.GetItemAsync(Tissue, CancellationToken.None)).Quantity);
    }

    [Fact]
    public async Task Overview_lists_empty_items_and_flags_low_and_expiring()
    {
        await using var ctx = new InventoryTestContext();
        await ctx.BuyAsync(Today.AddDays(-20), Line(Milk, 1, expiry: Today.AddDays(-1)));   // 已過期
        await ctx.BuyAsync(Today, Line(Milk, 1, expiry: Today.AddDays(5)));

        var all = await ctx.Stock.GetOverviewAsync(new StockQuery(), CancellationToken.None);
        var milk = all.Items.Single(r => r.ItemId == Milk);

        Assert.Equal(["衛生紙", "鮮奶"], all.Items.Select(r => r.Name).Order());   // 停用且沒庫存的舊肥皂不列出
        Assert.Equal(0m, all.Items.Single(r => r.ItemId == Tissue).Quantity);
        Assert.Equal(2m, milk.Quantity);
        Assert.Equal(1m, milk.ExpiredQuantity);
        Assert.Equal(ExpiryStatus.Expired, milk.ExpiryStatus);
        Assert.True(milk.IsLow);                                                  // 沒過期的只有 1，低於安全庫存 2

        var expiring = await ctx.Stock.GetOverviewAsync(new StockQuery { Status = StockStatusFilter.Expiring }, CancellationToken.None);
        Assert.Equal([Milk], expiring.Items.Select(r => r.ItemId));
        var empty = await ctx.Stock.GetOverviewAsync(new StockQuery { Status = StockStatusFilter.OutOfStock }, CancellationToken.None);
        Assert.Equal([Tissue], empty.Items.Select(r => r.ItemId));
    }

    [Fact]
    public async Task Updating_batch_quantity_records_an_adjustment()
    {
        await using var ctx = new InventoryTestContext();
        var purchase = await ctx.BuyAsync(Today, Line(Tissue, 3));

        var detail = await ctx.Stock.UpdateBatchAsync(
            purchase.Lines[0].BatchId,
            new UpdateBatchRequest(Quantity: 5, LocationId: Fridge, ExpiryDate: null, Note: "盤點"),
            CancellationToken.None);

        Assert.Equal(5m, detail.Quantity);
        Assert.Equal("冰箱冷藏", detail.Batches[0].LocationName);
        var adjust = detail.History.Single(t => t.Type == StockTransactionType.Adjust);
        Assert.Equal(2m, adjust.QuantityChange);
        Assert.Equal("盤點", adjust.Note);
    }

    [Theory]
    [InlineData(null, ExpiryStatus.None)]
    [InlineData(-1, ExpiryStatus.Expired)]
    [InlineData(0, ExpiryStatus.Soon)]
    [InlineData(7, ExpiryStatus.Soon)]
    [InlineData(8, ExpiryStatus.Upcoming)]
    [InlineData(30, ExpiryStatus.Upcoming)]
    [InlineData(31, ExpiryStatus.Ok)]
    public void Expiry_status_thresholds(int? daysFromToday, ExpiryStatus expected)
    {
        DateOnly? expiry = daysFromToday is { } days ? Today.AddDays(days) : null;
        Assert.Equal(expected, ExpiryRules.GetStatus(expiry, Today));
    }
}
