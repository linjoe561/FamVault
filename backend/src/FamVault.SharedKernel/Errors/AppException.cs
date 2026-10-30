using Microsoft.AspNetCore.Http;

namespace FamVault.SharedKernel.Errors;

/// <summary>
/// 可預期的業務錯誤。由 AppExceptionHandler 轉成對應 HTTP 狀態碼的 ProblemDetails。
/// </summary>
public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
    public abstract string Title { get; }
}

public sealed class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public override string Title => "找不到資料";
}

public sealed class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public override string Title => "資料衝突";
}

public sealed class BusinessRuleException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public override string Title => "無法完成操作";
}

public sealed class UnauthorizedException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
    public override string Title => "未通過驗證";
}
