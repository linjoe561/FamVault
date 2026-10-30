using FamVault.Modules.Inventory.Application;
using FamVault.Modules.Inventory.Infrastructure;
using FamVault.SharedKernel.Contracts;
using FamVault.SharedKernel.Modules;
using FamVault.SharedKernel.Persistence;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamVault.Modules.Inventory;

/// <summary>
/// 購買登錄、庫存批次與異動紀錄。購買與批次放在同一個模組，
/// 因為登錄購買時必須在同一次存檔中建立批次，不能只成功一半。
/// </summary>
public sealed class InventoryModule : IModule
{
    public string Name => "Inventory";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<InventoryModuleDbContext>(configuration, InventoryModuleDbContext.Schema);

        services.AddScoped<PurchaseService>();
        services.AddScoped<StockService>();
        services.AddScoped<ICatalogReferenceChecker, InventoryReferenceChecker>();
        services.AddValidatorsFromAssembly(typeof(InventoryModule).Assembly, includeInternalTypes: true);
    }
}
