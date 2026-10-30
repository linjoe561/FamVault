using FamVault.Modules.Catalog.Application;
using FamVault.SharedKernel.Auth;
using FamVault.SharedKernel.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FamVault.Modules.Catalog.Api;

// 分類、位置、單位：所有成員都能讀取（填寫品項時要用），只有管理者能新增、修改、刪除。

[ApiController]
[Route(ApiRoutes.Prefix + "/categories")]
public sealed class CategoriesController(CategoryService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CategoryDto>> List(CancellationToken cancellationToken) =>
        service.ListAsync(cancellationToken);

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(SaveCategoryRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, cancellationToken));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    public Task<CategoryDto> Update(int id, SaveCategoryRequest request, CancellationToken cancellationToken) =>
        service.UpdateAsync(id, request, cancellationToken);

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route(ApiRoutes.Prefix + "/locations")]
public sealed class LocationsController(LocationService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LocationDto>> List(CancellationToken cancellationToken) =>
        service.ListAsync(cancellationToken);

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<LocationDto>> Create(SaveLocationRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, cancellationToken));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    public Task<LocationDto> Update(int id, SaveLocationRequest request, CancellationToken cancellationToken) =>
        service.UpdateAsync(id, request, cancellationToken);

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route(ApiRoutes.Prefix + "/units")]
public sealed class UnitsController(UnitService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UnitDto>> List(CancellationToken cancellationToken) =>
        service.ListAsync(cancellationToken);

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<UnitDto>> Create(SaveUnitRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, cancellationToken));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    public Task<UnitDto> Update(int id, SaveUnitRequest request, CancellationToken cancellationToken) =>
        service.UpdateAsync(id, request, cancellationToken);

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
