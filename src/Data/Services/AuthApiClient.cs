using System.Text;
using System.Text.Json;
using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Data.Dtos;

namespace CargoPulse.Mobile.Data.Services;

public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        using var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(ApiConstants.LoginEndpoint, requestContent, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Network error occurred while connecting to authentication endpoint: {ex.Message}", ex, ex.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = string.IsNullOrWhiteSpace(responseBody)
                ? $"Authentication failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase})."
                : $"Authentication failed with HTTP {(int)response.StatusCode}: {responseBody}";

            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }

        var result = JsonSerializer.Deserialize<LoginResponseDto>(responseBody, JsonOptions);
        if (result is null)
        {
            throw new HttpRequestException("Failed to deserialize authentication response from server.", null, response.StatusCode);
        }

        return result;
    }
}
