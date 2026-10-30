using System.Text.RegularExpressions;
using FluentValidation;

namespace FamVault.Modules.Catalog.Application;

internal static class CatalogRules
{
    public const int BarcodeMaxLength = 50;
    public const int MaxBarcodesPerItem = 10;

    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule.Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("請輸入名稱")
            .MaximumLength(maxLength).WithMessage($"名稱最多 {maxLength} 個字");
}

internal sealed class SaveCategoryRequestValidator : AbstractValidator<SaveCategoryRequest>
{
    public SaveCategoryRequestValidator()
    {
        RuleFor(x => x.Name).ValidName(50);
    }
}

internal sealed class SaveLocationRequestValidator : AbstractValidator<SaveLocationRequest>
{
    public SaveLocationRequestValidator()
    {
        RuleFor(x => x.Name).ValidName(50);
        RuleFor(x => x.Description).MaximumLength(200).WithMessage("說明最多 200 個字");
    }
}

internal sealed class SaveUnitRequestValidator : AbstractValidator<SaveUnitRequest>
{
    public SaveUnitRequestValidator()
    {
        RuleFor(x => x.Name).ValidName(10);
    }
}

internal sealed partial class SaveItemRequestValidator : AbstractValidator<SaveItemRequest>
{
    public SaveItemRequestValidator()
    {
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.UnitId).GreaterThan(0).WithMessage("請選擇單位");
        RuleFor(x => x.SafetyStock)
            .InclusiveBetween(0m, 99999m).WithMessage("安全庫存量需介於 0～99999");
        RuleFor(x => x.ShelfLifeDays)
            .InclusiveBetween(1, 3650).WithMessage("保存天數需介於 1～3650 天");
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("備註最多 500 個字");
        RuleFor(x => x.Barcodes)
            .Must(codes => codes is null || codes.Count <= CatalogRules.MaxBarcodesPerItem)
            .WithMessage($"每個品項最多 {CatalogRules.MaxBarcodesPerItem} 組條碼");
        // 掃描或貼上的條碼常帶前後空白，存檔時會去掉，所以檢查時也先去掉
        RuleForEach(x => x.Barcodes)
            .Cascade(CascadeMode.Stop)
            .Must(code => !string.IsNullOrWhiteSpace(code)).WithMessage("條碼不能是空白")
            .Must(code => code.Trim().Length <= CatalogRules.BarcodeMaxLength)
            .WithMessage($"條碼最多 {CatalogRules.BarcodeMaxLength} 個字元")
            .Must(code => BarcodePattern().IsMatch(code.Trim())).WithMessage("條碼只能包含英文字母、數字與 -");
    }

    [GeneratedRegex("^[0-9A-Za-z-]+$")]
    private static partial Regex BarcodePattern();
}

internal sealed class ItemQueryValidator : AbstractValidator<ItemQuery>
{
    public ItemQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("頁碼需大於 0");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("每頁筆數需介於 1～200");
    }
}
