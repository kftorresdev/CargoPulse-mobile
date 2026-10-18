using CargoPulse.Mobile.Data.Local.Entities;
using SQLite;

namespace CargoPulse.Mobile.Data.Local;

public class LocalInspectionRepository : ILocalInspectionRepository
{
    private readonly string _databasePath;
    private SQLiteAsyncConnection? _connection;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public LocalInspectionRepository(string? databasePath = null)
    {
        _databasePath = string.IsNullOrWhiteSpace(databasePath)
            ? Path.Combine(FileSystem.AppDataDirectory, "cargopulse_local.db")
            : databasePath;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            if (_connection == null)
            {
                var directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _connection = new SQLiteAsyncConnection(_databasePath);
            }

            await _connection.CreateTableAsync<LocalInspectionRecord>().ConfigureAwait(false);
            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync().ConfigureAwait(false);
        }

        return _connection!;
    }

    public async Task SaveInspectionAsync(LocalInspectionRecord inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);

        var connection = await GetConnectionAsync().ConfigureAwait(false);
        await connection.InsertOrReplaceAsync(inspection).ConfigureAwait(false);
    }

    public async Task<LocalInspectionRecord?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var connection = await GetConnectionAsync().ConfigureAwait(false);
        return await connection.FindAsync<LocalInspectionRecord>(id).ConfigureAwait(false);
    }

    public async Task<List<LocalInspectionRecord>> GetPendingInspectionsAsync()
    {
        var connection = await GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<LocalInspectionRecord>()
            .Where(x => x.SyncStatus == "PENDING")
            .OrderBy(x => x.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<List<LocalInspectionRecord>> GetAllInspectionsAsync()
    {
        var connection = await GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<LocalInspectionRecord>()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task MarkAsSyncedAsync(string id, DateTime syncedAt)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var connection = await GetConnectionAsync().ConfigureAwait(false);
        var record = await connection.FindAsync<LocalInspectionRecord>(id).ConfigureAwait(false);
        if (record != null)
        {
            record.SyncStatus = "SYNCED";
            record.SyncedAt = syncedAt;
            record.LastError = null;
            await connection.UpdateAsync(record).ConfigureAwait(false);
        }
    }

    public async Task MarkAsFailedAsync(string id, string error)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var connection = await GetConnectionAsync().ConfigureAwait(false);
        var record = await connection.FindAsync<LocalInspectionRecord>(id).ConfigureAwait(false);
        if (record != null)
        {
            record.SyncStatus = "FAILED";
            record.LastError = error;
            record.RetryCount++;
            await connection.UpdateAsync(record).ConfigureAwait(false);
        }
    }

    public async Task<int> GetPendingCountAsync()
    {
        var connection = await GetConnectionAsync().ConfigureAwait(false);
        return await connection.Table<LocalInspectionRecord>()
            .Where(x => x.SyncStatus == "PENDING")
            .CountAsync()
            .ConfigureAwait(false);
    }
}
