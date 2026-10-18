using System.Globalization;
using Microsoft.Maui.Devices.Sensors;

namespace CargoPulse.Mobile.Core.Services;

public class GeolocationService : IGeolocationService
{
    private readonly IGeolocation _geolocation;

    public GeolocationService(IGeolocation? geolocation = null)
    {
        _geolocation = geolocation ?? Geolocation.Default;
    }

    public async Task<string?> GetCurrentCoordinatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Intentar obtener primero la última ubicación conocida en caché (~5 ms)
            try
            {
                var lastLocation = await _geolocation.GetLastKnownLocationAsync().ConfigureAwait(false);
                if (lastLocation != null)
                {
                    return $"{lastLocation.Latitude.ToString("F4", CultureInfo.InvariantCulture)},{lastLocation.Longitude.ToString("F4", CultureInfo.InvariantCulture)}";
                }
            }
            catch
            {
                // Continuar con solicitud en vivo si falla la lectura de la caché
            }

            // 2. Consultar ubicación en vivo con un tiempo límite estricto de 2 segundos para evitar congelamientos de UI
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(2));
            var locationTask = _geolocation.GetLocationAsync(request, linkedCts.Token);

            var completedTask = await Task.WhenAny(locationTask, Task.Delay(2000, linkedCts.Token)).ConfigureAwait(false);

            if (completedTask == locationTask)
            {
                var location = await locationTask.ConfigureAwait(false);
                if (location != null)
                {
                    return $"{location.Latitude.ToString("F4", CultureInfo.InvariantCulture)},{location.Longitude.ToString("F4", CultureInfo.InvariantCulture)}";
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
