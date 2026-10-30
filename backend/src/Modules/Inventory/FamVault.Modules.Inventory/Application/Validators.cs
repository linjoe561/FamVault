using FluentValidation;

namespace FamVault.Modules.Inventory.Application;

internal static class InventoryRules
{
    public const decimal MaxQuantity = 99999m;
    public const int MaxLinesPerPurchase = 50;

    public static IRuleBuilderOptions<T, decimal> ValidQuantity<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0).WithMessage("數量需大於 0")
            .LessThanOrEqualTo(MaxQuantity).WithMessage($"數量不能超過 {MaxQuantity}")
            .Must(HasAtMostThreeDecimals).WithMessage("數量最多到小數點後 3 位");

    public static bool HasAtMostThreeDecimals(decimal value) => decimal.Round(value, 3) == value;
}

internal sealed class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseRequestValidator()
    {
        RuleFor(x => x.PurchaseDate).NotEmpty().WithMessage("請選擇購買日期");
        RuleFor(x => x.Store).MaximumLength(50).WithMessage("店家最多 50 個字");
        RuleFor(x => x.Note).MaximumLength(200).WithMessage("備註最多 200 個字");
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("至少要有一樣東西")
            .Must(lines => lines is null || lines.Count <= InventoryRules.MaxLinesPerPurchase)
            .WithMessage($"一次最多登錄 {InventoryRules.MaxLinesPerPurchase} 樣");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).GreaterThan(0).WithMessage("請選擇品項");
            line.RuleFor(l => l.Quantity).ValidQuantity();
            line.RuleFor(l => l.UnitPrice)
                .InclusiveBetween(0m, 9999999m).WithMessage("單價需介於 0～9999999");
        });
    }
}

internal sealed class ConsumeRequestValidator : AbstractValidator<ConsumeRequest>
{
    public ConsumeRequestValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0).WithMessage("請選擇品項");
        RuleFor(x => x.Quantity).ValidQuantity();
        RuleFor(x => x.Kind).IsInEnum().WithMessage("類型不正確");
        RuleFor(x => x.Note).MaximumLength(200).WithMessage("備註最多 200 個字");
    }
}

internal sealed class UpdateBatchRequestValidator : AbstractValidator<UpdateBatchRequest>
{
    public UpdateBatchRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("數量不能小於 0")
            .LessThanOrEqualTo(InventoryRules.MaxQuantity).WithMessage($"數量不能超過 {InventoryRules.MaxQuantity}")
            .Must(InventoryRules.HasAtMostThreeDecimals).WithMessage("數量最多到小數點後 3 位");
        RuleFor(x => x.Note).MaximumLength(200).WithMessage("備註最多 200 個字");
    }
}

internal sealed class StockQueryValidator : AbstractValidator<StockQuery>
{
    public StockQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("狀態不正確");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("頁碼需大於 0");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("每頁筆數需介於 1～200");
    }
}

internal sealed class PurchaseQueryValidator : AbstractValidator<PurchaseQuery>
{
    public PurchaseQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("頁碼需大於 0");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每頁筆數需介於 1～100");
    }
}
