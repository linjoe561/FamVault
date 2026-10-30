using FamVault.Modules.Catalog.Application;
using FamVault.Modules.Catalog.Infrastructure;
using FamVault.SharedKernel.Modules;
using FamVault.SharedKernel.Persistence;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamVault.Modules.Catalog;

/// <summary>主檔：分類、存放位置、單位、品項與條碼。</summary>
public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<CatalogModuleDbContext>(configuration, CatalogModuleDbContext.Schema);

        services.AddScoped<CategoryService>();
        services.AddScoped<LocationService>();
        services.AddScoped<UnitService>();
        services.AddScoped<ItemService>();
        services.AddValidatorsFromAssembly(typeof(CatalogModule).Assembly, includeInternalTypes: true);
    }
}
