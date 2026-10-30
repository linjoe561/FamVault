using FamVault.Modules.Catalog.Application;
using FamVault.SharedKernel.Auth;
using FamVault.SharedKernel.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamVault.Modules.Catalog.Api;

/// <summary>品項：所有成員都可以查詢、新增與修改；永久刪除只限管理者。</summary>
[ApiController]
[Route(ApiRoutes.Prefix + "/items")]
public sealed class ItemsController(ItemService itemService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ItemDto>> List([FromQuery] ItemQuery query, CancellationToken cancellationToken) =>
        itemService.ListAsync(query, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<ItemDto> Get(int id, CancellationToken cancellationToken) =>
        itemService.GetAsync(id, cancellationToken);

    [HttpGet("by-barcode/{code}")]
    public Task<ItemDto> GetByBarcode(string code, CancellationToken cancellationToken) =>
        itemService.GetByBarcodeAsync(code, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(SaveItemRequest request, CancellationToken cancellationToken)
    {
        var item = await itemService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public Task<ItemDto> Update(int id, SaveItemRequest request, CancellationToken cancellationToken) =>
        itemService.UpdateAsync(id, request, cancellationToken);

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await itemService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
