using InvControl.Modules.Identity.Application;
using InvControl.Modules.Identity.Domain;
using InvControl.SharedKernel.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvControl.Modules.Identity.Api;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route(ApiRoutes.Prefix + "/users")]
public sealed class UsersController(UserService userService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UserDto>> List(CancellationToken cancellationToken) =>
        userService.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<UserDto> Get(Guid id) => userService.GetAsync(id);

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var user = await userService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    public Task<UserDto> Update(Guid id, UpdateUserRequest request) => userService.UpdateAsync(id, request);

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request)
    {
        await userService.ResetPasswordAsync(id, request);
        return NoContent();
    }
}
