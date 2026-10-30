using FamVault.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Catalog.Infrastructure;

/// <summary>
/// 第一次建立資料庫時的預設分類、位置與單位，家人可以再自行修改或刪除。
/// HasData 的值必須是常數，否則每次都會產生新的 Migration。
/// </summary>
internal static class CatalogSeedData
{
    private static readonly DateTimeOffset SeededAt = new(2026, 10, 30, 0, 0, 0, TimeSpan.Zero);

    private static readonly string[] Categories = ["食品", "飲料", "調味料", "日用品", "清潔用品", "個人保養", "藥品"];

    private static readonly string[] Locations = ["冰箱冷藏", "冰箱冷凍", "廚房櫃", "儲藏室", "浴室"];

    private static readonly string[] Units =
        ["個", "瓶", "罐", "包", "盒", "袋", "條", "卷", "片", "公斤", "公克", "公升", "毫升"];

    public static void Apply(ModelBuilder builder)
    {
        builder.Entity<Category>().HasData(Categories.Select((name, i) =>
            new Category { Id = i + 1, Name = name, SortOrder = i + 1, CreatedAt = SeededAt }));
        builder.Entity<Location>().HasData(Locations.Select((name, i) =>
            new Location { Id = i + 1, Name = name, SortOrder = i + 1, CreatedAt = SeededAt }));
        builder.Entity<Unit>().HasData(Units.Select((name, i) =>
            new Unit { Id = i + 1, Name = name, SortOrder = i + 1, CreatedAt = SeededAt }));
    }
}
