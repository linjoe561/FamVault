using System.Globalization;
using FluentValidation;
using InvControl.SharedKernel.Auth;
using InvControl.SharedKernel.Persistence;
using InvControl.SharedKernel.Web;
using Microsoft.Extensions.DependencyInjection;

namespace InvControl.SharedKernel;

public static class SharedKernelServiceCollectionExtensions
{
    /// <summary>註冊所有模組共用的基礎服務。需在註冊各模組之前呼叫。</summary>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("zh-TW");

        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddProblemDetails();
        services.AddExceptionHandler<AppExceptionHandler>();
        return services;
    }
}
