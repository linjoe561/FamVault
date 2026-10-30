using FluentValidation;
using InvControl.Modules.Identity.Domain;

namespace InvControl.Modules.Identity.Application;

internal static class IdentityRules
{
    public const int PasswordMinLength = 8;

    public static IRuleBuilderOptions<T, string> ValidUserName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("請輸入帳號")
            .Length(3, 30).WithMessage("帳號長度需為 3～30 個字元")
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("帳號只能使用英文字母、數字與 . _ -");

    public static IRuleBuilderOptions<T, string> ValidDisplayName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("請輸入顯示名稱")
            .MaximumLength(50).WithMessage("顯示名稱最多 50 個字元");

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("請輸入密碼")
            .MinimumLength(PasswordMinLength).WithMessage($"密碼至少需要 {PasswordMinLength} 個字元");

    public static IRuleBuilderOptions<T, string> ValidRole<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(role => RoleNames.All.Contains(role)).WithMessage("角色不正確");
}

internal sealed class SetupRequestValidator : AbstractValidator<SetupRequest>
{
    public SetupRequestValidator()
    {
        RuleFor(x => x.UserName).ValidUserName();
        RuleFor(x => x.DisplayName).ValidDisplayName();
        RuleFor(x => x.Password).ValidPassword();
    }
}

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("請輸入帳號");
        RuleFor(x => x.Password).NotEmpty().WithMessage("請輸入密碼");
    }
}

internal sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("缺少 Refresh Token");
    }
}

internal sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName).ValidUserName();
        RuleFor(x => x.DisplayName).ValidDisplayName();
        RuleFor(x => x.Password).ValidPassword();
        RuleFor(x => x.Role).ValidRole();
    }
}

internal sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.DisplayName).ValidDisplayName();
        RuleFor(x => x.Role).ValidRole();
    }
}

internal sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).ValidPassword();
    }
}

internal sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("請輸入目前密碼");
        RuleFor(x => x.NewPassword).ValidPassword();
    }
}
