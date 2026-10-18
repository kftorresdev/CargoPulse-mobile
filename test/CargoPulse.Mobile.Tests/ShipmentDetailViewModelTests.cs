using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using CargoPulse.Mobile.ViewModels;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class ShipmentDetailViewModelTests
{
    private readonly Mock<IShipmentApiClient> _mockShipmentApiClient;
    private readonly ShipmentDetailViewModel _sut;

    public ShipmentDetailViewModelTests()
    {
        _mockShipmentApiClient = new Mock<IShipmentApiClient>();
        _sut = new ShipmentDetailViewModel(_mockShipmentApiClient.Object);
    }

    [Fact]
    public void ShipmentDetailViewModel_InitialState_HasExpectedDefaults()
    {
        _sut.ShipmentId.Should().BeEmpty();
        _sut.Shipment.Should().BeNull();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.HasAlerts.Should().BeFalse();
        _sut.AlertsCount.Should().Be(0);
        _sut.IsBusy.Should().BeFalse();
        _sut.Title.Should().Be("Detalle del Embarque");
    }

    [Fact]
    public async Task LoadShipmentAsync_WhenSuccessful_PopulatesShipmentAndAlerts()
    {
        // Arrange
        const string shipmentId = "ship-101";
        var alerts = new List<AlertDto>
        {
            new AlertDto("alt-1", shipmentId, "Sello violado", "Contenedor roto", "CRITICAL", "OPEN", DateTime.UtcNow, 500, null, null),
            new AlertDto("alt-2", shipmentId, "Retraso frontera", "Demora en aduana", "MEDIUM", "OPEN", DateTime.UtcNow, 100, null, null)
        };
        var stages = new List<FreightStageDto>
        {
            new FreightStageDto("stg-1", shipmentId, "ORIGIN_HANDLING", 1, 1000, 1000, 0, 0, "USD", "APPROVED", null)
        };
        var shipment = new ShipmentDto(
            Id: shipmentId,
            TrackingNumber: "TRK-SUCCESS-101",
            Origin: "Guadalajara Hub",
            Destination: "Nuevo Laredo Garita",
            CurrentStatus: "CUSTOMS_BORDER",
            CarrierName: "Fletes de Occidente",
            Eta: DateTime.UtcNow.AddHours(5),
            ActualDelivery: null,
            AlertsCount: 2,
            Stages: stages,
            Alerts: alerts
        );
        var response = new ShipmentDetailResponseDto(
            Success: true,
            Data: shipment,
            Timestamp: DateTime.UtcNow
        );

        _mockShipmentApiClient
            .Setup(api => api.GetShipmentByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _sut.LoadShipmentAsync(shipmentId);

        // Assert
        _sut.Shipment.Should().NotBeNull();
        _sut.Shipment!.Id.Should().Be(shipmentId);
        _sut.Shipment.TrackingNumber.Should().Be("TRK-SUCCESS-101");
        _sut.ShipmentId.Should().Be(shipmentId);
        _sut.AlertsCount.Should().Be(2);
        _sut.HasAlerts.Should().BeTrue();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.IsBusy.Should().BeFalse();
        _mockShipmentApiClient.Verify(api => api.GetShipmentByIdAsync(shipmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadShipmentAsync_WhenFailed_SetsErrorMessage()
    {
        // Arrange
        const string shipmentId = "ship-error-404";
        _mockShipmentApiClient
            .Setup(api => api.GetShipmentByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Embarque no encontrado en servidor"));

        // Act
        await _sut.LoadShipmentAsync(shipmentId);

        // Assert
        _sut.Shipment.Should().BeNull();
        _sut.ErrorMessage.Should().Contain("Error al cargar el embarque");
        _sut.ErrorMessage.Should().Contain("Embarque no encontrado en servidor");
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadShipmentAsync_WhenDataIsNull_SetsErrorMessage()
    {
        // Arrange
        const string shipmentId = "ship-null-data";
        var response = new ShipmentDetailResponseDto(
            Success: false,
            Data: null!,
            Timestamp: DateTime.UtcNow
        );

        _mockShipmentApiClient
            .Setup(api => api.GetShipmentByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _sut.LoadShipmentAsync(shipmentId);

        // Assert
        _sut.Shipment.Should().BeNull();
        _sut.ErrorMessage.Should().Be("No se encontró el detalle del embarque solicitado.");
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.IsBusy.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task LoadShipmentAsync_WhenIdIsNullOrWhitespace_SetsErrorMessageWithoutCallingApi(string? invalidId)
    {
        // Act
        await _sut.LoadShipmentAsync(invalidId!);

        // Assert
        _sut.ErrorMessage.Should().Be("Identificador de embarque inválido.");
        _sut.HasErrorMessage.Should().BeTrue();
        _mockShipmentApiClient.Verify(api => api.GetShipmentByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HasAlerts_ReturnsTrue_WhenAlertsCountGreaterThanZero()
    {
        // Arrange: Shipment with alerts
        const string shipmentId = "ship-has-alerts";
        var shipment = new ShipmentDto(
            Id: shipmentId,
            TrackingNumber: "TRK-ALERTS",
            Origin: "Origin",
            Destination: "Dest",
            CurrentStatus: "IN_TRANSIT",
            CarrierName: "Carrier",
            Eta: DateTime.UtcNow,
            ActualDelivery: null,
            AlertsCount: 1,
            Stages: null,
            Alerts: new List<AlertDto>
            {
                new AlertDto("alt-1", shipmentId, "Alerta", "Detalle", "HIGH", "OPEN", null, 0, null, null)
            }
        );

        _mockShipmentApiClient
            .Setup(api => api.GetShipmentByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShipmentDetailResponseDto(true, shipment, DateTime.UtcNow));

        // Act
        await _sut.LoadShipmentAsync(shipmentId);

        // Assert
        _sut.AlertsCount.Should().Be(1);
        _sut.HasAlerts.Should().BeTrue();
    }

    [Fact]
    public async Task HasAlerts_ReturnsFalse_WhenAlertsCountIsZero()
    {
        // Arrange: Shipment without alerts
        const string shipmentId = "ship-no-alerts";
        var shipment = new ShipmentDto(
            Id: shipmentId,
            TrackingNumber: "TRK-CLEAN",
            Origin: "Origin",
            Destination: "Dest",
            CurrentStatus: "DELIVERED",
            CarrierName: "Carrier",
            Eta: DateTime.UtcNow,
            ActualDelivery: DateTime.UtcNow,
            AlertsCount: 0,
            Stages: null,
            Alerts: new List<AlertDto>()
        );

        _mockShipmentApiClient
            .Setup(api => api.GetShipmentByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShipmentDetailResponseDto(true, shipment, DateTime.UtcNow));

        // Act
        await _sut.LoadShipmentAsync(shipmentId);

        // Assert
        _sut.AlertsCount.Should().Be(0);
        _sut.HasAlerts.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyQueryAttributes_ExtractsIdAndTriggersLoadShipment()
    {
        // Arrange
        const string queryId = "ship-from-query";
        var shipment = new ShipmentDto(
            Id: queryId,
            TrackingNumber: "TRK-QUERY",
            Origin: "Origin",
            Destination: "Dest",
            CurrentStatus: "PLANNED",
            CarrierName: "Carrier",
            Eta: DateTime.UtcNow,
            ActualDelivery: null,
            AlertsCount: 0,
            Stages: null,
            Alerts: null
        );

        _mockShipmentApiClient
            .Setup(api => api.GetShipmentByIdAsync(queryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShipmentDetailResponseDto(true, shipment, DateTime.UtcNow));

        var query = new Dictionary<string, object>
        {
            { "id", queryId }
        };

        // Act
        _sut.ApplyQueryAttributes(query);

        // Give async Task time to execute
        await Task.Delay(50);

        // Assert
        _sut.ShipmentId.Should().Be(queryId);
        _mockShipmentApiClient.Verify(api => api.GetShipmentByIdAsync(queryId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshCommand_WhenShipmentIdPresent_ReloadsData()
    {
        // Arrange
        const string shipmentId = "ship-refresh";
        var shipment = new ShipmentDto(
            Id: shipmentId,
            TrackingNumber: "TRK-REFRESH",
            Origin: "Origin",
            Destination: "Dest",
            CurrentStatus: "IN_TRANSIT",
            CarrierName: "Carrier",
            Eta: DateTime.UtcNow,
            ActualDelivery: null,
            AlertsCount: 0,
            Stages: null,
            Alerts: null
        );

        _mockShipmentApiClient
            .Setup(api => api.GetShipmentByIdAsync(shipmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShipmentDetailResponseDto(true, shipment, DateTime.UtcNow));

        _sut.ShipmentId = shipmentId;

        // Act
        await _sut.RefreshCommand.ExecuteAsync(null);

        // Assert
        _sut.Shipment.Should().NotBeNull();
        _mockShipmentApiClient.Verify(api => api.GetShipmentByIdAsync(shipmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NavigationCommands_ExecuteWithoutThrowingWhenShellCurrentIsNull()
    {
        // Arrange
        _sut.Shipment = new ShipmentDto(
            Id: "ship-inspect",
            TrackingNumber: "TRK-INSPECT",
            Origin: "Origin",
            Destination: "Dest",
            CurrentStatus: "IN_TRANSIT",
            CarrierName: "Carrier",
            Eta: DateTime.UtcNow,
            ActualDelivery: null,
            AlertsCount: 0,
            Stages: null,
            Alerts: null
        );

        // Act & Assert
        await _sut.StartInspectionCommand.ExecuteAsync(null);
        await _sut.GoBackCommand.ExecuteAsync(null);

        _sut.IsBusy.Should().BeFalse();
    }
}
