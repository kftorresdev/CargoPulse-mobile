namespace CargoPulse.Mobile.Core.Services;

public class SyncProgressEventArgs : EventArgs
{
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface ISyncEngine
{
    bool IsSyncing { get; }
    event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
    Task<int> SyncPendingInspectionsAsync(CancellationToken cancellationToken = default);
}
