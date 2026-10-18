using Microsoft.Maui.Storage;

namespace CargoPulse.Mobile.Core.Services;

public class SecureStorageService : ISecureStorageService
{
    private readonly ISecureStorage _secureStorage;

    public SecureStorageService(ISecureStorage? secureStorage = null)
    {
        _secureStorage = secureStorage ?? SecureStorage.Default;
    }

    public async Task<string?> GetAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return await _secureStorage.GetAsync(key);
    }

    public async Task SetAsync(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        await _secureStorage.SetAsync(key, value);
    }

    public bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _secureStorage.Remove(key);
    }

    public void RemoveAll()
    {
        _secureStorage.RemoveAll();
    }
}
