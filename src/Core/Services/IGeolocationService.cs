namespace CargoPulse.Mobile.Core.Services;

public interface IGeolocationService
{
    Task<string?> GetCurrentCoordinatesAsync(CancellationToken cancellationToken = default);
}
