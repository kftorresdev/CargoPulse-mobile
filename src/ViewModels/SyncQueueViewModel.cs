using System.Collections.ObjectModel;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Local;
using CargoPulse.Mobile.Data.Local.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoPulse.Mobile.ViewModels;

public partial class SyncQueueViewModel : BaseViewModel, IDisposable
{
    private readonly ILocalInspectionRepository _localInspectionRepository;
    private readonly ISyncEngine _syncEngine;
    private readonly IConnectivityService _connectivityService;
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<LocalInspectionRecord> _inspections = new();

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private bool _isOnline;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private string _syncMessage = string.Empty;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private bool _isEmpty = true;

    public SyncQueueViewModel(
        ILocalInspectionRepository localInspectionRepository,
        ISyncEngine syncEngine,
        IConnectivityService connectivityService)
    {
        _localInspectionRepository = localInspectionRepository ?? throw new ArgumentNullException(nameof(localInspectionRepository));
        _syncEngine = syncEngine ?? throw new ArgumentNullException(nameof(syncEngine));
        _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));

        Title = "Cola de Sincronización";
        IsOnline = _connectivityService.IsConnected;
        IsSyncing = _syncEngine.IsSyncing;

        _syncEngine.SyncProgressChanged += OnSyncProgressChanged;
        _connectivityService.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        SyncMessage = e.Message ?? string.Empty;
        if (e.Total > 0)
        {
            ProgressPercent = Math.Clamp((double)e.Processed / e.Total, 0.0, 1.0);
        }
        else
        {
            ProgressPercent = 0.0;
        }

        IsSyncing = _syncEngine.IsSyncing;
    }

    private void OnConnectivityChanged(object? sender, bool isConnected)
    {
        if (_disposed)
        {
            return;
        }

        IsOnline = isConnected;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var records = await _localInspectionRepository.GetAllInspectionsAsync();

            Inspections.Clear();
            if (records != null)
            {
                foreach (var record in records)
                {
                    Inspections.Add(record);
                }
            }

            PendingCount = await _localInspectionRepository.GetPendingCountAsync();
            IsEmpty = Inspections.Count == 0;
        }
        catch (Exception ex)
        {
            SyncMessage = $"Error al cargar inspecciones: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ForceSyncAsync()
    {
        if (_disposed || IsBusy || IsSyncing)
        {
            return;
        }

        try
        {
            IsBusy = true;
            IsSyncing = true;
            ProgressPercent = 0.0;
            SyncMessage = "Iniciando sincronización...";

            if (!_connectivityService.IsConnected)
            {
                SyncMessage = "Modo offline: sin conexión para sincronizar.";
            }

            await _syncEngine.SyncPendingInspectionsAsync();
        }
        catch (Exception ex)
        {
            SyncMessage = $"Error durante la sincronización: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
            IsBusy = false;
            await RefreshAsync();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _syncEngine.SyncProgressChanged -= OnSyncProgressChanged;
            _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
        }

        _disposed = true;
    }
}

