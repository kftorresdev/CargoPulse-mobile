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
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            var location = await _geolocation.GetLocationAsync(request, cancellationToken);

            if (location != null)
            {
                return $"{location.Latitude.ToString("F4", CultureInfo.InvariantCulture)},{location.Longitude.ToString("F4", CultureInfo.InvariantCulture)}";
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
