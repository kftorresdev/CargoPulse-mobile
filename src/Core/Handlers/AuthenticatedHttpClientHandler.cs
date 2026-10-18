using System.Net.Http.Headers;
using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Core.Services;

namespace CargoPulse.Mobile.Core.Handlers;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly ISecureStorageService _secureStorageService;

    public AuthenticatedHttpClientHandler(ISecureStorageService secureStorageService)
        : this(secureStorageService, new HttpClientHandler())
    {
    }

    public AuthenticatedHttpClientHandler(ISecureStorageService secureStorageService, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _secureStorageService = secureStorageService ?? throw new ArgumentNullException(nameof(secureStorageService));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _secureStorageService.GetAsync(ApiConstants.AuthTokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
