namespace CargoPulse.Mobile.Core.Services;

public interface ISignatureService
{
    Task<string> ConvertDrawingStreamToBase64Async(Stream stream);
}
