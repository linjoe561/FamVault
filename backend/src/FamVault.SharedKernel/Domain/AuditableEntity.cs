namespace FamVault.SharedKernel.Domain;

/// <summary>
/// 需要記錄「誰、何時」建立與修改的資料。欄位由 AuditableEntityInterceptor 在存檔時自動填入。
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

/// <summary>
/// 一般業務資料的基底類別：整數主鍵加上稽核欄位。
/// </summary>
public abstract class AuditableEntity : IAuditable
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
