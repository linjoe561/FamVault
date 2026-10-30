namespace FamVault.Modules.Inventory.Domain;

public enum ExpiryStatus
{
    /// <summary>沒有到期日（例如衛生紙）</summary>
    None,

    /// <summary>已過期</summary>
    Expired,

    /// <summary>7 天內到期（含今天）</summary>
    Soon,

    /// <summary>30 天內到期</summary>
    Upcoming,

    /// <summary>還早</summary>
    Ok,
}

public static class ExpiryRules
{
    public const int SoonDays = 7;
    public const int UpcomingDays = 30;

    public static ExpiryStatus GetStatus(DateOnly? expiryDate, DateOnly today) => expiryDate switch
    {
        null => ExpiryStatus.None,
        { } date when date < today => ExpiryStatus.Expired,
        { } date when date <= today.AddDays(SoonDays) => ExpiryStatus.Soon,
        { } date when date <= today.AddDays(UpcomingDays) => ExpiryStatus.Upcoming,
        _ => ExpiryStatus.Ok,
    };
}
