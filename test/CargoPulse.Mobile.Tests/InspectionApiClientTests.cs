using System.Net;
using System.Text;
using System.Text.Json;
using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Core.Handlers;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class InspectionApiClientTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    private static CreateInspectionRequestDto CreateSampleRequestDto()
    {
        return new CreateInspectionRequestDto(
            Id: "insp-001",
            ShipmentId: "ship-999",
            ContainerCondition: "GOOD",
            SealIntact: true,
            SealNumberDeclared: "SEAL-A100",
            SealNumberActual: "SEAL-A100",
            SealMismatch: false,
            DamagesReported: false,
            DamagesDescription: null,
            Photos: new List<PhotoUploadDto>
            {
                new PhotoUploadDto("seal.jpg", "image/jpeg", "base64-seal-data")
            },
            SignatureBase64: "data:image/png;base64,sample-signature",
            SignerName: "Carlos Mendez",
            GpsCoordinates: "25.6866,-100.3161",
            InspectedAt: new DateTime(2026, 10, 17, 22, 0, 0, DateTimeKind.Utc)
        );
    }

    private static InspectionSyncResponseDto CreateSampleResponseDto()
    {
        return new InspectionSyncResponseDto(
            Success: true,
            Data: new InspectionSyncResultDto(
                Id: "insp-001",
                ShipmentId: "ship-999",
                Status: "SYNCED",
                SyncedAt: new DateTime(2026, 10, 17, 22, 1, 0, DateTimeKind.Utc),
                AlertCreated: false
            ),
            Timestamp: new DateTime(2026, 10, 17, 22, 1, 0, DateTimeKind.Utc)
        );
    }

    [Fact]
    public void Constructor_WhenHttpClientIsNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new InspectionApiClient(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public async Task UploadInspectionAsync_WhenDtoIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new InspectionApiClient(httpClient);

        // Act
        var act = async () => await client.UploadInspectionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("dto");
    }

    [Fact]
    public async Task UploadInspectionAsync_WhenSuccessful_PostsToInspectionsEndpointAndReturnsResponse()
    {
        // Arrange
        HttpRequestMessage? interceptedRequest = null;
        string? interceptedBody = null;
        var requestDto = CreateSampleRequestDto();
        var expectedResponse = CreateSampleResponseDto();

        var handler = new FakeHttpMessageHandler(req =>
        {
            interceptedRequest = req;
            interceptedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();

            var json = JsonSerializer.Serialize(expectedResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new InspectionApiClient(httpClient);

        // Act
        var result = await client.UploadInspectionAsync(requestDto);

        // Assert
        interceptedRequest.Should().NotBeNull();
        interceptedRequest!.Method.Should().Be(HttpMethod.Post);
        interceptedRequest.RequestUri!.ToString().Should().Be($"http://localhost:3000/api/{ApiConstants.InspectionsEndpoint}");
        interceptedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");

        interceptedBody.Should().NotBeNullOrWhiteSpace();
        interceptedBody.Should().Contain("\"id\":\"insp-001\"");
        interceptedBody.Should().Contain("\"shipmentId\":\"ship-999\"");
        interceptedBody.Should().Contain("\"containerCondition\":\"GOOD\"");
        interceptedBody.Should().Contain("\"sealIntact\":true");
        interceptedBody.Should().Contain("\"sealNumberDeclared\":\"SEAL-A100\"");
        interceptedBody.Should().Contain("\"sealNumberActual\":\"SEAL-A100\"");
        interceptedBody.Should().Contain("\"sealMismatch\":false");
        interceptedBody.Should().Contain("\"damagesReported\":false");
        interceptedBody.Should().Contain("\"fileName\":\"seal.jpg\"");
        interceptedBody.Should().Contain("\"signerName\":\"Carlos Mendez\"");
        interceptedBody.Should().Contain("\"gpsCoordinates\":\"25.6866,-100.3161\"");

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be("insp-001");
        result.Data.ShipmentId.Should().Be("ship-999");
        result.Data.Status.Should().Be("SYNCED");
        result.Data.AlertCreated.Should().BeFalse();
    }

    [Fact]
    public async Task UploadInspectionAsync_WhenServerReturnsHttp400BadRequest_ThrowsHttpRequestExceptionWithStatusCodeAndContent()
    {
        // Arrange
        const string errorJson = "{\"error\":\"Validation failed: seal actual mismatch required\"}";
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new InspectionApiClient(httpClient);
        var requestDto = CreateSampleRequestDto();

        // Act
        var act = async () => await client.UploadInspectionAsync(requestDto);

        // Assert
        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("400");
        ex.Which.Message.Should().Contain(errorJson);
    }

    [Fact]
    public async Task UploadInspectionAsync_WhenServerReturnsHttp500InternalServerError_ThrowsHttpRequestExceptionWithStatusCode()
    {
        // Arrange
        const string errorContent = "Internal Server Error occurred";
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(errorContent, Encoding.UTF8, "text/plain")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new InspectionApiClient(httpClient);
        var requestDto = CreateSampleRequestDto();

        // Act
        var act = async () => await client.UploadInspectionAsync(requestDto);

        // Assert
        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ex.Which.Message.Should().Contain("500");
        ex.Which.Message.Should().Contain(errorContent);
    }

    [Fact]
    public async Task UploadInspectionAsync_WhenServerReturnsNonSuccessWithEmptyBody_ThrowsHttpRequestExceptionWithReasonPhrase()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Not Found",
            Content = new StringContent(string.Empty)
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new InspectionApiClient(httpClient);
        var requestDto = CreateSampleRequestDto();

        // Act
        var act = async () => await client.UploadInspectionAsync(requestDto);

        // Assert
        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Contain("404");
        ex.Which.Message.Should().Contain("Not Found");
    }

    [Fact]
    public async Task UploadInspectionAsync_WhenServerReturnsMalformedJson_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<!DOCTYPE html><html>Error</html>", Encoding.UTF8, "text/html")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new InspectionApiClient(httpClient);
        var requestDto = CreateSampleRequestDto();

        // Act
        var act = async () => await client.UploadInspectionAsync(requestDto);

        // Assert
        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.Message.Should().Contain("Failed to deserialize inspection sync response");
    }

    [Fact]
    public async Task UploadInspectionAsync_WhenServerReturnsNullJson_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new InspectionApiClient(httpClient);
        var requestDto = CreateSampleRequestDto();

        // Act
        var act = async () => await client.UploadInspectionAsync(requestDto);

        // Assert
        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.Message.Should().Contain("Failed to deserialize inspection sync response");
    }

    [Fact]
    public async Task UploadInspectionAsync_WhenNetworkFails_RethrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            throw new HttpRequestException("DNS resolution failed", null, HttpStatusCode.ServiceUnavailable));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new InspectionApiClient(httpClient);
        var requestDto = CreateSampleRequestDto();

        // Act
        var act = async () => await client.UploadInspectionAsync(requestDto);

        // Assert
        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.Message.Should().Contain("Network error occurred while uploading inspection");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task UploadInspectionAsync_WhenUsedWithAuthenticatedHttpClientHandler_IncludesBearerToken()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey))
            .ReturnsAsync("test-jwt-inspection-token");

        HttpRequestMessage? interceptedRequest = null;
        var expectedResponse = CreateSampleResponseDto();

        var innerHandler = new FakeHttpMessageHandler(req =>
        {
            interceptedRequest = req;
            var json = JsonSerializer.Serialize(expectedResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var authHandler = new AuthenticatedHttpClientHandler(mockStorage.Object, innerHandler);
        var httpClient = new HttpClient(authHandler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new InspectionApiClient(httpClient);

        // Act
        var result = await client.UploadInspectionAsync(CreateSampleRequestDto());

        // Assert
        result.Should().NotBeNull();
        interceptedRequest.Should().NotBeNull();
        interceptedRequest!.Headers.Authorization.Should().NotBeNull();
        interceptedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        interceptedRequest.Headers.Authorization.Parameter.Should().Be("test-jwt-inspection-token");
    }
}
