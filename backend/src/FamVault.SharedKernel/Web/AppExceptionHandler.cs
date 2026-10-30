using FamVault.SharedKernel.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamVault.SharedKernel.Web;

/// <summary>
/// 把例外統一轉成 ProblemDetails，前端只需要處理一種錯誤格式。
/// </summary>
internal sealed class AppExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            AppException app => (app.StatusCode, app.Title, app.Message),
            // 兩位家人同時修改同一筆資料（例如同時用掉同一批牛奶）
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict, "資料衝突", "這筆資料剛被其他人修改過，請重新整理後再試一次"),
            _ => (StatusCodes.Status500InternalServerError, "伺服器發生錯誤", null as string),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "未處理的例外");
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title, Detail = detail },
        });
    }
}
