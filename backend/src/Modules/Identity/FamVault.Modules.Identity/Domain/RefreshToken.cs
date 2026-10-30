namespace FamVault.Modules.Identity.Domain;

/// <summary>
/// 用來換發 Access Token 的長效憑證。資料庫只存 SHA-256 雜湊，不存原文。
/// 每次換發都會撤銷舊的並發一張新的（輪替）。
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
