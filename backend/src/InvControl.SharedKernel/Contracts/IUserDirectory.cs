namespace InvControl.SharedKernel.Contracts;

/// <summary>
/// 跨模組查詢使用者顯示名稱的合約，由 Identity 模組實作。
/// 其他模組只存使用者 Id，需要顯示「誰操作的」時透過這個介面查名稱，不直接讀 Identity 的資料表。
/// </summary>
public interface IUserDirectory
{
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
