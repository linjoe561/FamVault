using FamVault.Modules.Catalog.Domain;
using FamVault.Modules.Catalog.Infrastructure;
using FamVault.SharedKernel.Errors;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Catalog.Application;

public sealed class CategoryService(CatalogModuleDbContext db)
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken) =>
        await db.Categories
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.ParentId, db.Items.Count(i => i.CategoryId == c.Id)))
            .ToListAsync(cancellationToken);

    public async Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureValidParentAsync(request.ParentId, categoryId: null, cancellationToken);
        await EnsureNameAvailableAsync(name, request.ParentId, exceptId: null, cancellationToken);

        var category = new Category
        {
            Name = name,
            ParentId = request.ParentId,
            SortOrder = await db.Categories.MaxAsync(c => (int?)c.SortOrder, cancellationToken) + 1 ?? 1,
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return new CategoryDto(category.Id, category.Name, category.ParentId, 0);
    }

    public async Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureValidParentAsync(request.ParentId, id, cancellationToken);
        await EnsureNameAvailableAsync(name, request.ParentId, id, cancellationToken);

        category.Name = name;
        category.ParentId = request.ParentId;
        await db.SaveChangesAsync(cancellationToken);
        return new CategoryDto(
            category.Id, category.Name, category.ParentId,
            await db.Items.CountAsync(i => i.CategoryId == id, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var category = await FindAsync(id, cancellationToken);
        if (await db.Categories.AnyAsync(c => c.ParentId == id, cancellationToken))
        {
            throw new ConflictException($"「{category.Name}」底下還有小分類，請先刪除或移動它們");
        }

        var itemCount = await db.Items.CountAsync(i => i.CategoryId == id, cancellationToken);
        if (itemCount > 0)
        {
            throw new ConflictException($"還有 {itemCount} 個品項屬於「{category.Name}」，請先改成其他分類");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Categories.FindAsync([id], cancellationToken) ?? throw new NotFoundException("找不到這個分類");

    /// <summary>分類最多兩層：上層必須是大分類，而且有小分類的分類不能再被放到別人底下。</summary>
    private async Task EnsureValidParentAsync(int? parentId, int? categoryId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return;
        }

        if (parentId == categoryId)
        {
            throw new BusinessRuleException("分類不能放在自己底下");
        }

        var parent = await db.Categories.FindAsync([parentId.Value], cancellationToken)
            ?? throw new BusinessRuleException("找不到上層分類");
        if (parent.ParentId is not null)
        {
            throw new BusinessRuleException("分類最多兩層，小分類底下不能再放分類");
        }

        if (categoryId is not null && await db.Categories.AnyAsync(c => c.ParentId == categoryId, cancellationToken))
        {
            throw new BusinessRuleException("這個分類底下還有小分類，不能再放到其他分類底下");
        }
    }

    private async Task EnsureNameAvailableAsync(
        string name, int? parentId, int? exceptId, CancellationToken cancellationToken)
    {
        var taken = await db.Categories.AnyAsync(
            c => c.Name == name && c.ParentId == parentId && c.Id != exceptId, cancellationToken);
        if (taken)
        {
            throw new ConflictException($"已經有叫「{name}」的分類");
        }
    }
}
