using System.Text.Json;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Local;
using CargoPulse.Mobile.Data.Local.Entities;
using CargoPulse.Mobile.Data.Services;

namespace CargoPulse.Mobile.Core.Services;

public class BackgroundSyncEngine : ISyncEngine, IDisposable
{
    private readonly ILocalInspectionRepository _repository;
    private readonly IInspectionApiClient _apiClient;
    private readonly IConnectivityService _connectivityService;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private bool _disposed;

    public bool IsSyncing { get; private set; }

    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

    public BackgroundSyncEngine(
        ILocalInspectionRepository repository,
        IInspectionApiClient apiClient,
        IConnectivityService connectivityService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));

        _connectivityService.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, bool isConnected)
    {
        if (isConnected)
        {
            _ = SafeSyncPendingInspectionsAsync();
        }
    }

    private async Task SafeSyncPendingInspectionsAsync()
    {
        try
        {
            await SyncPendingInspectionsAsync().ConfigureAwait(false);
        }
        catch
        {
            // Suppress unhandled exceptions triggered from background event
        }
    }

    public async Task<int> SyncPendingInspectionsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || !_connectivityService.IsConnected || IsSyncing)
        {
            return 0;
        }

        if (!await _syncLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return 0;
        }

        try
        {
            IsSyncing = true;

            List<LocalInspectionRecord> pending;
            try
            {
                pending = await _repository.GetPendingInspectionsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs
                {
                    Total = 0,
                    Processed = 0,
                    Succeeded = 0,
                    Failed = 0,
                    Message = $"Error al consultar inspecciones pendientes: {ex.Message}"
                });
                return 0;
            }

            if (pending == null || pending.Count == 0)
            {
                return 0;
            }

            int total = pending.Count;
            int processed = 0;
            int succeeded = 0;
            int failed = 0;

            foreach (var item in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var photos = await LoadPhotosAsync(item.PhotosJson, cancellationToken).ConfigureAwait(false);

                    var requestDto = new CreateInspectionRequestDto(
                        Id: item.Id,
                        ShipmentId: item.ShipmentId,
                        ContainerCondition: item.ContainerCondition,
                        SealIntact: item.SealIntact,
                        SealNumberDeclared: item.DeclaredSealNumber,
                        SealNumberActual: item.ActualSealNumber,
                        SealMismatch: item.SealMismatch,
                        DamagesReported: item.DamagesReported,
                        DamagesDescription: item.DamagesDescription,
                        Photos: photos,
                        SignatureBase64: item.SignatureBase64,
                        SignerName: item.SignerName,
                        GpsCoordinates: item.GpsCoordinates,
                        InspectedAt: item.InspectedAt
                    );

                    var result = await _apiClient.UploadInspectionAsync(requestDto, cancellationToken).ConfigureAwait(false);

                    if (result == null || !result.Success || result.Data == null)
                    {
                        throw new InvalidOperationException("API response indicated failure or returned empty data.");
                    }

                    await _repository.MarkAsSyncedAsync(item.Id, result.Data.SyncedAt).ConfigureAwait(false);
                    succeeded++;
                    processed++;

                    SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs
                    {
                        Total = total,
                        Processed = processed,
                        Succeeded = succeeded,
                        Failed = failed,
                        Message = $"Inspección {item.Id} sincronizada con éxito ({processed}/{total})."
                    });
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    processed++;

                    try
                    {
                        await _repository.MarkAsFailedAsync(item.Id, ex.Message).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore persistence errors for failure reporting
                    }

                    SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs
                    {
                        Total = total,
                        Processed = processed,
                        Succeeded = succeeded,
                        Failed = failed,
                        Message = $"Error al sincronizar inspección {item.Id}: {ex.Message}"
                    });
                }
            }

            return succeeded;
        }
        finally
        {
            IsSyncing = false;
            _syncLock.Release();
        }
    }

    private static async Task<List<PhotoUploadDto>> LoadPhotosAsync(string? photosJson, CancellationToken cancellationToken)
    {
        var photos = new List<PhotoUploadDto>();

        if (string.IsNullOrWhiteSpace(photosJson) || photosJson.Trim() == "[]")
        {
            return photos;
        }

        try
        {
            using var doc = JsonDocument.Parse(photosJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return photos;
            }

            int index = 0;
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                index++;
                string? filePath = null;
                string? fileName = null;
                string? contentType = null;
                string? inlineContent = null;

                if (element.ValueKind == JsonValueKind.String)
                {
                    var str = element.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        if (File.Exists(str))
                        {
                            filePath = str;
                        }
                        else
                        {
                            inlineContent = str;
                        }
                    }
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    if (element.TryGetProperty("fileName", out var fn))
                    {
                        fileName = fn.GetString();
                    }

                    if (element.TryGetProperty("contentType", out var ct))
                    {
                        contentType = ct.GetString();
                    }

                    if (element.TryGetProperty("base64Content", out var b64))
                    {
                        var content = b64.GetString();
                        if (!string.IsNullOrWhiteSpace(content) && File.Exists(content))
                        {
                            filePath = content;
                        }
                        else
                        {
                            inlineContent = content;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(filePath) && element.TryGetProperty("filePath", out var fp))
                    {
                        var path = fp.GetString();
                        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                        {
                            filePath = path;
                        }
                    }
                }

                string base64String = string.Empty;
                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                {
                    byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
                    base64String = Convert.ToBase64String(bytes);
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        fileName = Path.GetFileName(filePath);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(inlineContent))
                {
                    base64String = inlineContent;
                }

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = $"photo_{index}.jpg";
                }

                if (string.IsNullOrWhiteSpace(contentType))
                {
                    contentType = GetContentType(fileName);
                }

                photos.Add(new PhotoUploadDto(fileName, contentType, base64String));
            }
        }
        catch (JsonException)
        {
            if (File.Exists(photosJson))
            {
                byte[] bytes = await File.ReadAllBytesAsync(photosJson, cancellationToken).ConfigureAwait(false);
                photos.Add(new PhotoUploadDto(
                    FileName: Path.GetFileName(photosJson),
                    ContentType: GetContentType(photosJson),
                    Base64Content: Convert.ToBase64String(bytes)
                ));
            }
        }

        return photos;
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
        _syncLock.Dispose();
    }
}
