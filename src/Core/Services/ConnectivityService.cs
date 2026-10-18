using Microsoft.Maui.Networking;

namespace CargoPulse.Mobile.Core.Services;

public class ConnectivityService : IConnectivityService, IDisposable
{
    private readonly IConnectivity? _connectivity;
    private readonly object _lock = new();
    private bool _disposed;

    public event EventHandler<bool>? ConnectivityChanged;

    public ConnectivityService() : this(GetDefaultConnectivity())
    {
    }

    public ConnectivityService(IConnectivity? connectivity)
    {
        _connectivity = connectivity;
        if (_connectivity != null)
        {
            try
            {
                _connectivity.ConnectivityChanged += OnMauiConnectivityChanged;
            }
            catch
            {
                // Headless or unsupported environment fallback
            }
        }
    }

    public bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                try
                {
                    if (_connectivity == null)
                    {
                        return true;
                    }

                    return _connectivity.NetworkAccess == NetworkAccess.Internet;
                }
                catch
                {
                    // Safe fallback for headless / test environments
                    return true;
                }
            }
        }
    }

    private void OnMauiConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        bool isConnected = e.NetworkAccess == NetworkAccess.Internet;
        ConnectivityChanged?.Invoke(this, isConnected);
    }

    private static IConnectivity? GetDefaultConnectivity()
    {
        try
        {
            return Connectivity.Current;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        if (_connectivity != null)
        {
            try
            {
                _connectivity.ConnectivityChanged -= OnMauiConnectivityChanged;
            }
            catch
            {
            }
        }
    }
}
