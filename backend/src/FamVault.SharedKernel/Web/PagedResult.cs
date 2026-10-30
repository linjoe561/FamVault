namespace FamVault.SharedKernel.Web;

/// <summary>分頁查詢結果。Page 從 1 開始。</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
