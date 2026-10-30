using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamVault.SharedKernel.Persistence;

/// <summary>
/// 每個模組的資料庫。啟動時由 Api 逐一執行 Migration。
/// </summary>
public interface IModuleDatabase
{
    string Name { get; }

    Task MigrateAsync(CancellationToken cancellationToken);
}

internal sealed class ModuleDatabase<TContext>(TContext context) : IModuleDatabase
    where TContext : DbContext
{
    public string Name => typeof(TContext).Name;

    public Task MigrateAsync(CancellationToken cancellationToken) => context.Database.MigrateAsync(cancellationToken);
}

public static class ModuleDatabaseExtensions
{
    /// <summary>
    /// 註冊模組自己的 DbContext。每個模組使用獨立的 schema 與 Migration 紀錄表，
    /// 模組之間不建立跨 schema 的外鍵，只用 Id 互相參照。
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services, IConfiguration configuration, string schema)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("缺少連線字串 ConnectionStrings:Default");

        services.AddDbContext<TContext>((sp, options) => options
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));
        services.AddScoped<IModuleDatabase, ModuleDatabase<TContext>>();
        return services;
    }

    public static async Task MigrateModuleDatabasesAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        foreach (var database in scope.ServiceProvider.GetServices<IModuleDatabase>())
        {
            await database.MigrateAsync(cancellationToken);
        }
    }
}
