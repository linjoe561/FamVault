using Microsoft.AspNetCore.Identity;

namespace FamVault.Modules.Identity.Infrastructure;

/// <summary>把 ASP.NET Core Identity 會用到的錯誤訊息換成中文。</summary>
internal sealed class ZhTwIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "發生未知的錯誤" };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"帳號「{userName}」已經有人使用" };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "帳號只能使用英文字母、數字與 . _ -" };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"密碼至少需要 {length} 個字元" };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "目前密碼不正確" };

    public override IdentityError InvalidToken() =>
        new() { Code = nameof(InvalidToken), Description = "重設密碼的憑證無效" };
}
