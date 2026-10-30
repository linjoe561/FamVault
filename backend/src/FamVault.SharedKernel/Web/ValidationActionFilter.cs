using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FamVault.SharedKernel.Web;

/// <summary>
/// 對每個 Action 參數尋找對應的 FluentValidation 驗證器並執行；
/// 驗證失敗時回傳 400 ValidationProblemDetails，欄位名稱轉成 camelCase 以對應前端。
/// </summary>
public sealed class ValidationActionFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 不直接寫進 ModelState：它的 key 不分大小寫，會沿用模型繫結時的 "Page" 而不是 "page"。
        var errors = new Dictionary<string, List<string>>();
        foreach (var (key, entry) in context.ModelState)
        {
            foreach (var error in entry.Errors)
            {
                AddError(errors, key, error.ErrorMessage);
            }
        }

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            foreach (var error in result.Errors)
            {
                AddError(errors, error.PropertyName, error.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(
                new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()))
                {
                    Title = "輸入資料有誤",
                    Status = 400,
                });
            return;
        }

        await next();
    }

    private static void AddError(Dictionary<string, List<string>> errors, string propertyName, string message)
    {
        // 巢狀欄位每一段都轉 camelCase：Lines[0].Quantity → lines[0].quantity
        var key = string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
        if (!errors.TryGetValue(key, out var messages))
        {
            errors[key] = messages = [];
        }

        messages.Add(message);
    }
}
