using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace CargoPulse.Mobile.Core.Services;

public class MediaService : IMediaService
{
    private const long MaxPhotoSizeBytes = 20 * 1024 * 1024; // 20 MB max photo size
    private readonly IMediaPicker _mediaPicker;

    public MediaService(IMediaPicker? mediaPicker = null)
    {
        _mediaPicker = mediaPicker ?? MediaPicker.Default;
    }

    public async Task<string?> CapturePhotoAsync(CancellationToken cancellationToken = default)
    {
        string? filePath = null;
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
            filePath = Path.Combine(evidenceDir, fileName);

            using var stream = await photo.OpenReadAsync();
            if (stream == null || !stream.CanRead)
            {
                return null;
            }

            if (stream.CanSeek && (stream.Length == 0 || stream.Length > MaxPhotoSizeBytes))
            {
                return null;
            }

            using var fileStream = File.Create(filePath);
            byte[] buffer = new byte[8192];
            long totalWritten = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                totalWritten += bytesRead;
                if (totalWritten > MaxPhotoSizeBytes)
                {
                    fileStream.Dispose();
                    TryDeleteFile(filePath);
                    return null;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            }

            await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);

            if (totalWritten == 0)
            {
                fileStream.Dispose();
                TryDeleteFile(filePath);
                return null;
            }

            return Path.GetFullPath(filePath);
        }
        catch (Exception)
        {
            if (filePath != null)
            {
                TryDeleteFile(filePath);
            }
            return null;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore file deletion failure
        }
    }
}

