using FamVault.Modules.Inventory.Application;
using FamVault.SharedKernel.Web;
using Microsoft.AspNetCore.Mvc;

namespace FamVault.Modules.Inventory.Api;

/// <summary>購買登錄：所有成員都可以登錄；刪除限登錄的人或管理者，而且還沒被用掉。</summary>
[ApiController]
[Route(ApiRoutes.Prefix + "/purchases")]
public sealed class PurchasesController(PurchaseService purchases) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<PurchaseDto>> List([FromQuery] PurchaseQuery query, CancellationToken cancellationToken) =>
        purchases.ListAsync(query, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<PurchaseDto> Get(int id, CancellationToken cancellationToken) =>
        purchases.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<PurchaseDto>> Create(CreatePurchaseRequest request, CancellationToken cancellationToken)
    {
        var purchase = await purchases.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = purchase.Id }, purchase);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await purchases.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

/// <summary>庫存查詢與異動：所有成員都可以使用，每筆異動會記錄操作人。</summary>
[ApiController]
[Route(ApiRoutes.Prefix + "/stock")]
public sealed class StockController(StockService stock) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<StockSummaryDto>> Overview([FromQuery] StockQuery query, CancellationToken cancellationToken) =>
        stock.GetOverviewAsync(query, cancellationToken);

    [HttpGet("items/{itemId:int}")]
    public Task<StockItemDetailDto> Item(int itemId, CancellationToken cancellationToken) =>
        stock.GetItemAsync(itemId, cancellationToken);

    [HttpPost("consume")]
    public Task<StockItemDetailDto> Consume(ConsumeRequest request, CancellationToken cancellationToken) =>
        stock.ConsumeAsync(request, cancellationToken);

    [HttpPut("batches/{id:int}")]
    public Task<StockItemDetailDto> UpdateBatch(int id, UpdateBatchRequest request, CancellationToken cancellationToken) =>
        stock.UpdateBatchAsync(id, request, cancellationToken);
}
