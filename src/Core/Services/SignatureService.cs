namespace CargoPulse.Mobile.Core.Services;

public class SignatureService : ISignatureService
{
    public async Task<string> ConvertDrawingStreamToBase64Async(Stream stream)
    {
        try
        {
            if (stream == null || !stream.CanRead)
            {
                return string.Empty;
            }

            using var memoryStream = new MemoryStream();
            if (stream.CanSeek && stream.Position != 0)
            {
                stream.Position = 0;
            }

            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            if (bytes.Length == 0)
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
}
