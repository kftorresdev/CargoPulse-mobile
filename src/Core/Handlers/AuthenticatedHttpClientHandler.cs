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

        // Dynamically align request URI with currently configured ApiConstants.BaseUrl
        var currentBaseUrl = ApiConstants.BaseUrl;
        if (request.RequestUri != null)
        {
            var targetBase = new Uri(currentBaseUrl);
            if (!request.RequestUri.IsAbsoluteUri)
            {
                request.RequestUri = new Uri(targetBase, request.RequestUri);
            }
            else if (!string.Equals(request.RequestUri.Host, targetBase.Host, StringComparison.OrdinalIgnoreCase) ||
                     request.RequestUri.Port != targetBase.Port ||
                     !string.Equals(request.RequestUri.Scheme, targetBase.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                var uriBuilder = new UriBuilder(request.RequestUri)
                {
                    Scheme = targetBase.Scheme,
                    Host = targetBase.Host,
                    Port = targetBase.Port
                };
                request.RequestUri = uriBuilder.Uri;
            }
        }

        var token = await _secureStorageService.GetAsync(ApiConstants.AuthTokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
