namespace CargoPulse.Mobile.Core.Services;

public interface IMediaService
{
    Task<string?> CapturePhotoAsync(CancellationToken cancellationToken = default);
}
