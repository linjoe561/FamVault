using Microsoft.Extensions.Configuration;

namespace FamVault.SharedKernel.Time;

/// <summary>
/// 「今天」是哪一天要以家裡所在的時區計算（預設台北），不能用伺服器的 UTC 日期，
/// 否則晚上 8 點前登錄的東西會被算成前一天。
/// </summary>
public interface IAppClock
{
    DateTimeOffset Now { get; }
    DateOnly Today { get; }
}

internal sealed class AppClock(TimeProvider timeProvider, IConfiguration configuration) : IAppClock
{
    private readonly TimeZoneInfo timeZone =
        TimeZoneInfo.FindSystemTimeZoneById(configuration["App:TimeZone"] ?? "Asia/Taipei");

    public DateTimeOffset Now => timeProvider.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Now, timeZone).DateTime);
}
