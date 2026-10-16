namespace Foto_CreaDB2;

public sealed class DeletionStatistics
{
    /// <summary>Candidates preserved because validation, access or deletion failed.</summary>
    public int FilesBlockedCount { get; internal set; }
    public int FilesMissingCount { get; internal set; }
    public int DatabaseErrorsCount { get; internal set; }
}
