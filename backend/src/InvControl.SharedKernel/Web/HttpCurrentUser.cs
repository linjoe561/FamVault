using InvControl.SharedKernel.Auth;
using Microsoft.AspNetCore.Http;

namespace InvControl.SharedKernel.Web;

internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(AppClaimTypes.UserId)?.Value, out var id) ? id : null;

    public string? DisplayName => accessor.HttpContext?.User.FindFirst(AppClaimTypes.DisplayName)?.Value;

    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;
}
