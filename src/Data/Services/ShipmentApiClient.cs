using System.Text.Json;
using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Data.Dtos;

namespace CargoPulse.Mobile.Data.Services;

public class ShipmentApiClient : IShipmentApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ShipmentApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<ShipmentListResponseDto> GetShipmentsAsync(
        int page = 1,
        int limit = 10,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{ApiConstants.ShipmentsEndpoint}?page={page}&limit={limit}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search.Trim())}";
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            url += $"&status={Uri.EscapeDataString(status.Trim())}";
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Network error occurred while fetching shipments: {ex.Message}", ex, ex.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = string.IsNullOrWhiteSpace(responseBody)
                ? $"Fetching shipments failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase})."
                : $"Fetching shipments failed with HTTP {(int)response.StatusCode}: {responseBody}";

            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }

        var result = JsonSerializer.Deserialize<ShipmentListResponseDto>(responseBody, JsonOptions);
        if (result is null)
        {
            throw new HttpRequestException("Failed to deserialize shipment list response from server.", null, response.StatusCode);
        }

        return result;
    }

    public async Task<ShipmentDetailResponseDto> GetShipmentByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var url = $"{ApiConstants.ShipmentsEndpoint}/{Uri.EscapeDataString(id.Trim())}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Network error occurred while fetching shipment detail: {ex.Message}", ex, ex.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = string.IsNullOrWhiteSpace(responseBody)
                ? $"Fetching shipment failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase})."
                : $"Fetching shipment failed with HTTP {(int)response.StatusCode}: {responseBody}";

            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }

        var result = JsonSerializer.Deserialize<ShipmentDetailResponseDto>(responseBody, JsonOptions);
        if (result is null)
        {
            throw new HttpRequestException("Failed to deserialize shipment detail response from server.", null, response.StatusCode);
        }

        return result;
    }

    public async Task<ShipmentDetailResponseDto?> FindByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            return null;
        }

        var cleanTracking = trackingNumber.Trim();
        var listResponse = await GetShipmentsAsync(page: 1, limit: 5, search: cleanTracking, cancellationToken: cancellationToken);

        var items = listResponse?.Data?.Items;
        if (items == null || items.Count == 0)
        {
            return null;
        }

        var match = items.FirstOrDefault(s =>
                        string.Equals(s.TrackingNumber?.Trim(), cleanTracking, StringComparison.OrdinalIgnoreCase))
                    ?? items.FirstOrDefault(s =>
                        s.TrackingNumber?.Contains(cleanTracking, StringComparison.OrdinalIgnoreCase) == true)
                    ?? items.FirstOrDefault();

        if (match == null || string.IsNullOrWhiteSpace(match.Id))
        {
            return null;
        }

        return await GetShipmentByIdAsync(match.Id, cancellationToken);
    }
}
