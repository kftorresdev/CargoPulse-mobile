using System.Net;
using System.Text;
using System.Text.Json;
using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using FluentAssertions;

namespace CargoPulse.Mobile.Tests;

public class ShipmentApiClientTests
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

    private static ShipmentDto CreateTestShipment(
        string id = "ship-1",
        string trackingNumber = "TRK-1001",
        string status = "IN_TRANSIT")
    {
        return new ShipmentDto(
            Id: id,
            TrackingNumber: trackingNumber,
            Origin: "Shanghai Port",
            Destination: "Los Angeles Port",
            CurrentStatus: status,
            CarrierName: "Pacific Ocean Line",
            Eta: DateTime.UtcNow.AddDays(5),
            ActualDelivery: null,
            AlertsCount: 1,
            Stages: new List<FreightStageDto>
            {
                new FreightStageDto(
                    Id: "stage-1",
                    ShipmentId: id,
                    StageType: "OCEAN_TRANSIT",
                    StageOrder: 1,
                    ContractedCost: 5000,
                    ActualCost: 5200,
                    Variance: 200,
                    VariancePercent: 4.0,
                    Currency: "USD",
                    Status: "ACTIVE",
                    AuditNotes: "On schedule"
                )
            },
            Alerts: new List<AlertDto>
            {
                new AlertDto(
                    Id: "alert-1",
                    ShipmentId: id,
                    Title: "Minor Weather Delay",
                    Description: "Heavy seas in Pacific",
                    Severity: "LOW",
                    Status: "ACTIVE",
                    SlaDeadline: DateTime.UtcNow.AddHours(24),
                    FinancialImpactUsd: 0,
                    ResolutionNotes: null,
                    ResolvedAt: null
                )
            }
        );
    }

    #region GetShipmentsAsync Tests

    [Fact]
    public async Task GetShipmentsAsync_ReturnsPaginatedList()
    {
        await GetShipmentsAsync_WithDefaults_ConstructsExpectedUriAndReturnsDto();
    }

    [Fact]
    public async Task GetShipmentsAsync_WithDefaults_ConstructsExpectedUriAndReturnsDto()
    {
        // Arrange
        HttpRequestMessage? interceptedRequest = null;
        var shipment = CreateTestShipment();
        var expectedResponse = new ShipmentListResponseDto(
            Success: true,
            Data: new ShipmentListDataDto(
                Items: new List<ShipmentDto> { shipment },
                Meta: new PaginationMetaDto(Total: 1, Page: 1, Limit: 10, TotalPages: 1)
            ),
            Timestamp: DateTime.UtcNow
        );

        var handler = new FakeHttpMessageHandler(req =>
        {
            interceptedRequest = req;
            var json = JsonSerializer.Serialize(expectedResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new ShipmentApiClient(httpClient);

        // Act
        var result = await client.GetShipmentsAsync();

        // Assert
        interceptedRequest.Should().NotBeNull();
        interceptedRequest!.Method.Should().Be(HttpMethod.Get);
        interceptedRequest.RequestUri!.ToString().Should().Be("http://localhost:3000/api/shipments?page=1&limit=10");
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items[0].TrackingNumber.Should().Be("TRK-1001");
        result.Data.Meta.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetShipmentsAsync_WithSearchAndStatus_BuildsCorrectQueryParameters()
    {
        // Arrange
        HttpRequestMessage? interceptedRequest = null;
        var expectedResponse = new ShipmentListResponseDto(
            Success: true,
            Data: new ShipmentListDataDto(
                Items: new List<ShipmentDto>(),
                Meta: new PaginationMetaDto(Total: 0, Page: 2, Limit: 25, TotalPages: 0)
            ),
            Timestamp: DateTime.UtcNow
        );

        var handler = new FakeHttpMessageHandler(req =>
        {
            interceptedRequest = req;
            var json = JsonSerializer.Serialize(expectedResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new ShipmentApiClient(httpClient);

        // Act
        var result = await client.GetShipmentsAsync(page: 2, limit: 25, search: "TRK 900", status: "CUSTOMS_HOLD");

        // Assert
        interceptedRequest.Should().NotBeNull();
        interceptedRequest!.RequestUri!.PathAndQuery.Should().Be("/api/shipments?page=2&limit=25&search=TRK%20900&status=CUSTOMS_HOLD");
        result.Data.Meta.Page.Should().Be(2);
        result.Data.Meta.Limit.Should().Be(25);
    }

    [Fact]
    public async Task GetShipmentsAsync_WhenHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"error\":\"Internal Server Error\"}", Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new ShipmentApiClient(httpClient);

        // Act
        var act = async () => await client.GetShipmentsAsync();

        // Assert
        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetShipmentsAsync_WhenResponseInvalid_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new ShipmentApiClient(httpClient);

        // Act
        var act = async () => await client.GetShipmentsAsync();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    #endregion

    #region GetShipmentByIdAsync Tests

    [Fact]
    public async Task GetShipmentByIdAsync_ReturnsCorrectDetail()
    {
        await GetShipmentByIdAsync_WhenFound_ReturnsShipmentDetailResponseDto();
    }

    [Fact]
    public async Task GetShipmentByIdAsync_WhenFound_ReturnsShipmentDetailResponseDto()
    {
        // Arrange
        HttpRequestMessage? interceptedRequest = null;
        var shipment = CreateTestShipment(id: "ship-777", trackingNumber: "TRK-777");
        var expectedResponse = new ShipmentDetailResponseDto(
            Success: true,
            Data: shipment,
            Timestamp: DateTime.UtcNow
        );

        var handler = new FakeHttpMessageHandler(req =>
        {
            interceptedRequest = req;
            var json = JsonSerializer.Serialize(expectedResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new ShipmentApiClient(httpClient);

        // Act
        var result = await client.GetShipmentByIdAsync("ship-777");

        // Assert
        interceptedRequest.Should().NotBeNull();
        interceptedRequest!.Method.Should().Be(HttpMethod.Get);
        interceptedRequest.RequestUri!.ToString().Should().Be("http://localhost:3000/api/shipments/ship-777");
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be("ship-777");
        result.Data.TrackingNumber.Should().Be("TRK-777");
        result.Data.Stages.Should().NotBeNull();
        result.Data.Stages.Should().HaveCount(1);
        result.Data.Alerts.Should().NotBeNull();
        result.Data.Alerts.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetShipmentByIdAsync_WhenNotFound_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\":\"Shipment not found\"}", Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new ShipmentApiClient(httpClient);

        // Act
        var act = async () => await client.GetShipmentByIdAsync("unknown-id");

        // Assert
        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetShipmentByIdAsync_WhenIdIsNullOrWhitespace_ThrowsArgumentException(string? invalidId)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new ShipmentApiClient(httpClient);

        var act = async () => await client.GetShipmentByIdAsync(invalidId!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region FindByTrackingNumberAsync Tests

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task FindByTrackingNumberAsync_WhenNullOrWhitespace_ReturnsNull(string? invalidTracking)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new ShipmentApiClient(httpClient);

        var result = await client.FindByTrackingNumberAsync(invalidTracking!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByTrackingNumberAsync_WhenNotFound_ReturnsNull()
    {
        await FindByTrackingNumberAsync_WhenNoItemsReturned_ReturnsNull();
    }

    [Fact]
    public async Task FindByTrackingNumberAsync_WhenNoItemsReturned_ReturnsNull()
    {
        // Arrange
        var emptyListResponse = new ShipmentListResponseDto(
            Success: true,
            Data: new ShipmentListDataDto(
                Items: new List<ShipmentDto>(),
                Meta: new PaginationMetaDto(Total: 0, Page: 1, Limit: 5, TotalPages: 0)
            ),
            Timestamp: DateTime.UtcNow
        );

        var handler = new FakeHttpMessageHandler(req =>
        {
            var json = JsonSerializer.Serialize(emptyListResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new ShipmentApiClient(httpClient);

        // Act
        var result = await client.FindByTrackingNumberAsync("NON-EXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByTrackingNumberAsync_WhenFound_ReturnsDetail()
    {
        await FindByTrackingNumberAsync_WhenExactMatchFound_CallsGetByIdAndReturnsFullDetails();
    }

    [Fact]
    public async Task FindByTrackingNumberAsync_WhenExactMatchFound_CallsGetByIdAndReturnsFullDetails()
    {
        // Arrange
        var listResponse = new ShipmentListResponseDto(
            Success: true,
            Data: new ShipmentListDataDto(
                Items: new List<ShipmentDto>
                {
                    CreateTestShipment(id: "ship-other", trackingNumber: "TRK-1001-ALT"),
                    CreateTestShipment(id: "ship-exact", trackingNumber: "TRK-1001")
                },
                Meta: new PaginationMetaDto(Total: 2, Page: 1, Limit: 5, TotalPages: 1)
            ),
            Timestamp: DateTime.UtcNow
        );

        var fullShipmentDetail = new ShipmentDetailResponseDto(
            Success: true,
            Data: CreateTestShipment(id: "ship-exact", trackingNumber: "TRK-1001"),
            Timestamp: DateTime.UtcNow
        );

        var requestedUrls = new List<string>();

        var handler = new FakeHttpMessageHandler(req =>
        {
            var url = req.RequestUri!.ToString();
            requestedUrls.Add(url);

            if (url.Contains("page=1&limit=5&search=TRK-1001"))
            {
                var json = JsonSerializer.Serialize(listResponse);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            if (url.EndsWith("shipments/ship-exact"))
            {
                var json = JsonSerializer.Serialize(fullShipmentDetail);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new ShipmentApiClient(httpClient);

        // Act
        var result = await client.FindByTrackingNumberAsync("TRK-1001");

        // Assert
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Id.Should().Be("ship-exact");
        result.Data.TrackingNumber.Should().Be("TRK-1001");
        requestedUrls.Should().HaveCount(2);
        requestedUrls[0].Should().Contain("page=1&limit=5&search=TRK-1001");
        requestedUrls[1].Should().EndWith("shipments/ship-exact");
    }

    [Fact]
    public async Task FindByTrackingNumberAsync_WhenPartialMatchFound_SelectsPartialMatchAndReturnsDetails()
    {
        // Arrange
        var listResponse = new ShipmentListResponseDto(
            Success: true,
            Data: new ShipmentListDataDto(
                Items: new List<ShipmentDto>
                {
                    CreateTestShipment(id: "ship-partial", trackingNumber: "CARGO-TRK-8888-EXP")
                },
                Meta: new PaginationMetaDto(Total: 1, Page: 1, Limit: 5, TotalPages: 1)
            ),
            Timestamp: DateTime.UtcNow
        );

        var fullDetail = new ShipmentDetailResponseDto(
            Success: true,
            Data: CreateTestShipment(id: "ship-partial", trackingNumber: "CARGO-TRK-8888-EXP"),
            Timestamp: DateTime.UtcNow
        );

        var handler = new FakeHttpMessageHandler(req =>
        {
            var url = req.RequestUri!.ToString();
            if (url.EndsWith("shipments/ship-partial"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(fullDetail), Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(listResponse), Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var client = new ShipmentApiClient(httpClient);

        // Act
        var result = await client.FindByTrackingNumberAsync("8888");

        // Assert
        result.Should().NotBeNull();
        result!.Data.Id.Should().Be("ship-partial");
        result.Data.TrackingNumber.Should().Be("CARGO-TRK-8888-EXP");
    }

    #endregion
}
