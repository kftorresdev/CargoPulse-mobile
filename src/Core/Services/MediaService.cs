using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace CargoPulse.Mobile.Core.Services;

public class MediaService : IMediaService
{
    private readonly IMediaPicker _mediaPicker;

    public MediaService(IMediaPicker? mediaPicker = null)
    {
        _mediaPicker = mediaPicker ?? MediaPicker.Default;
    }

    public async Task<string?> CapturePhotoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var photo = await _mediaPicker.CapturePhotoAsync();
            if (photo == null)
            {
                return null;
            }

            var evidenceDir = Path.Combine(FileSystem.AppDataDirectory, "evidence");
            if (!Directory.Exists(evidenceDir))
            {
                Directory.CreateDirectory(evidenceDir);
            }

            var fileName = $"evidence_{Guid.NewGuid():N}.jpg";
            var filePath = Path.Combine(evidenceDir, fileName);

            using (var stream = await photo.OpenReadAsync())
            using (var fileStream = File.Create(filePath))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            return Path.GetFullPath(filePath);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
