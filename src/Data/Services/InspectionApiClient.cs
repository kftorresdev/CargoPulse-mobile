using System.Text;
using System.Text.Json;
using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Data.Dtos;

namespace CargoPulse.Mobile.Data.Services;

public class InspectionApiClient : IInspectionApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InspectionApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<InspectionSyncResponseDto> UploadInspectionAsync(
        CreateInspectionRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var requestJson = JsonSerializer.Serialize(dto, JsonOptions);
        using var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(ApiConstants.InspectionsEndpoint, requestContent, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Network error occurred while uploading inspection: {ex.Message}", ex, ex.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = string.IsNullOrWhiteSpace(responseBody)
                ? $"Uploading inspection failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase})."
                : $"Uploading inspection failed with HTTP {(int)response.StatusCode}: {responseBody}";

            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }

        InspectionSyncResponseDto? result;
        try
        {
            result = JsonSerializer.Deserialize<InspectionSyncResponseDto>(responseBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException($"Failed to deserialize inspection sync response from server: {ex.Message}", ex, response.StatusCode);
        }

        if (result is null)
        {
            throw new HttpRequestException("Failed to deserialize inspection sync response from server.", null, response.StatusCode);
        }

        return result;
    }
}
