using System.Globalization;
using FluentValidation;
using FamVault.SharedKernel.Auth;
using FamVault.SharedKernel.Persistence;
using FamVault.SharedKernel.Time;
using FamVault.SharedKernel.Web;
using Microsoft.Extensions.DependencyInjection;

namespace FamVault.SharedKernel;

public static class SharedKernelServiceCollectionExtensions
{
    /// <summary>註冊所有模組共用的基礎服務。需在註冊各模組之前呼叫。</summary>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("zh-TW");

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAppClock, AppClock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddProblemDetails();
        services.AddExceptionHandler<AppExceptionHandler>();
        return services;
    }
}
