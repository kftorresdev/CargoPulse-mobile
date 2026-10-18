namespace CargoPulse.Mobile.Core.Services;

public class SignatureService : ISignatureService
{
    private const long MaxSignatureSizeBytes = 5 * 1024 * 1024; // 5 MB max signature size
    private const int MinValidImageHeaderBytes = 4;

    public async Task<string> ConvertDrawingStreamToBase64Async(Stream stream)
    {
        try
        {
            if (stream == null || !stream.CanRead)
            {
                return string.Empty;
            }

            if (stream.CanSeek)
            {
                if (stream.Length == 0 || stream.Length > MaxSignatureSizeBytes)
                {
                    return string.Empty;
                }

                if (stream.Position != 0)
                {
                    stream.Position = 0;
                }
            }

            using var memoryStream = new MemoryStream();
            byte[] buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                totalRead += bytesRead;
                if (totalRead > MaxSignatureSizeBytes)
                {
                    return string.Empty;
                }

                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
            }

            var bytes = memoryStream.ToArray();

            if (bytes.Length < MinValidImageHeaderBytes || !IsValidImageHeader(bytes))
            {
                return string.Empty;
            }

            return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static bool IsValidImageHeader(byte[] bytes)
    {
        if (bytes.Length < MinValidImageHeaderBytes)
        {
            return false;
        }

        // PNG: 89 50 4E 47 (.PNG)
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return true;
        }

        // JPEG: FF D8 FF
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return true;
        }

        // GIF: 47 49 46 (GIF)
        if (bytes.Length >= 3 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
        {
            return true;
        }

        // BMP: 42 4D (BM)
        if (bytes[0] == 0x42 && bytes[1] == 0x4D)
        {
            return true;
        }

        // WebP/RIFF: 52 49 46 46 (RIFF)
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46)
        {
            return true;
        }

        return false;
    }
}

