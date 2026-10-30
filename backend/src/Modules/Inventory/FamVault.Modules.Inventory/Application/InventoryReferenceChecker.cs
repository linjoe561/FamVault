using FamVault.Modules.Inventory.Infrastructure;
using FamVault.SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Inventory.Application;

/// <summary>告訴 Catalog：有庫存紀錄的品項、還放著庫存的位置不能刪除。</summary>
internal sealed class InventoryReferenceChecker(InventoryModuleDbContext db) : ICatalogReferenceChecker
{
    public async Task<string?> GetItemDeleteBlockerAsync(int itemId, CancellationToken cancellationToken = default) =>
        await db.StockBatches.AnyAsync(b => b.ItemId == itemId, cancellationToken)
            ? "這個品項已經有購買或庫存紀錄，不能刪除；不再購買的話請改用「停用」"
            : null;

    public async Task<string?> GetLocationDeleteBlockerAsync(int locationId, CancellationToken cancellationToken = default)
    {
        var count = await db.StockBatches.CountAsync(
            b => b.LocationId == locationId && b.RemainingQuantity > 0, cancellationToken);
        return count > 0 ? $"還有 {count} 批庫存放在這個位置，請先移到其他位置" : null;
    }
}
