using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using CargoPulse.Mobile.ViewModels;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class ScannerViewModelTests
{
    private readonly Mock<IShipmentApiClient> _mockShipmentApiClient;
    private readonly Mock<IHapticService> _mockHapticService;
    private readonly ScannerViewModel _sut;

    public ScannerViewModelTests()
    {
        _mockShipmentApiClient = new Mock<IShipmentApiClient>();
        _mockHapticService = new Mock<IHapticService>();
        _sut = new ScannerViewModel(_mockShipmentApiClient.Object, _mockHapticService.Object);
    }

    [Fact]
    public void ScannerViewModel_InitialState_HasExpectedDefaults()
    {
        _sut.IsScanning.Should().BeTrue();
        _sut.IsTorchOn.Should().BeFalse();
        _sut.DetectedBarcode.Should().BeEmpty();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.IsBusy.Should().BeFalse();
        _sut.Title.Should().Be("Escáner Óptico");
    }

    [Fact]
    public async Task ProcessBarcodeAsync_WhenShipmentFound_InvokesHapticsAndNavigates()
    {
        // Arrange
        const string barcode = "CP-789012";
        var shipment = new ShipmentDto(
            Id: "ship-001",
            TrackingNumber: barcode,
            Origin: "Monterrey Hub",
            Destination: "Laredo Yard",
            CurrentStatus: "IN_TRANSIT",
            CarrierName: "Transportes Frontera",
            Eta: DateTime.UtcNow.AddDays(1),
            ActualDelivery: null,
            AlertsCount: 0,
            Stages: null,
            Alerts: null
        );
        var response = new ShipmentDetailResponseDto(
            Success: true,
            Data: shipment,
            Timestamp: DateTime.UtcNow
        );

        _mockShipmentApiClient
            .Setup(api => api.FindByTrackingNumberAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _sut.ProcessBarcodeAsync(barcode);

        // Assert
        _mockHapticService.Verify(h => h.Success(), Times.Once);
        _sut.IsScanning.Should().BeFalse();
        _sut.DetectedBarcode.Should().Be(barcode);
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.IsBusy.Should().BeFalse();
        _mockShipmentApiClient.Verify(api => api.FindByTrackingNumberAsync(barcode, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBarcodeAsync_WhenShipmentNotFound_SetsErrorMessageAndDoesNotNavigate()
    {
        // Arrange
        const string barcode = "UNKNOWN-999";
        _mockShipmentApiClient
            .Setup(api => api.FindByTrackingNumberAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShipmentDetailResponseDto?)null);

        // Act
        await _sut.ProcessBarcodeAsync(barcode);

        // Assert
        _mockHapticService.Verify(h => h.Success(), Times.Once);
        _sut.IsScanning.Should().BeFalse();
        _sut.DetectedBarcode.Should().Be(barcode);
        _sut.ErrorMessage.Should().Be($"No se encontró el embarque con tracking: {barcode}");
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.IsBusy.Should().BeFalse();
        _mockShipmentApiClient.Verify(api => api.FindByTrackingNumberAsync(barcode, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBarcodeAsync_WhenApiThrows_SetsErrorMessageAndDoesNotThrow()
    {
        // Arrange
        const string barcode = "FAIL-ERROR";
        _mockShipmentApiClient
            .Setup(api => api.FindByTrackingNumberAsync(barcode, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Server unavailable"));

        // Act
        await _sut.ProcessBarcodeAsync(barcode);

        // Assert
        _mockHapticService.Verify(h => h.Success(), Times.Once);
        _sut.IsScanning.Should().BeFalse();
        _sut.ErrorMessage.Should().Be($"No se encontró el embarque con tracking: {barcode}");
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.IsBusy.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ProcessBarcodeAsync_WhenBarcodeIsNullOrWhitespace_DoesNothing(string? invalidBarcode)
    {
        // Act
        await _sut.ProcessBarcodeAsync(invalidBarcode!);

        // Assert
        _mockHapticService.Verify(h => h.Success(), Times.Never);
        _mockShipmentApiClient.Verify(api => api.FindByTrackingNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _sut.IsScanning.Should().BeTrue();
        _sut.DetectedBarcode.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBarcodeDetected_ForwardsToProcessBarcodeAsync()
    {
        // Arrange
        const string barcode = "CP-FORWARD";
        var shipment = new ShipmentDto(
            Id: "ship-002",
            TrackingNumber: barcode,
            Origin: "Hub",
            Destination: "Yard",
            CurrentStatus: "DRAFT",
            CarrierName: "Carrier",
            Eta: DateTime.UtcNow,
            ActualDelivery: null,
            AlertsCount: 0,
            Stages: null,
            Alerts: null
        );

        _mockShipmentApiClient
            .Setup(api => api.FindByTrackingNumberAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShipmentDetailResponseDto(true, shipment, DateTime.UtcNow));

        // Act
        await _sut.ProcessBarcodeDetected(barcode);

        // Assert
        _sut.DetectedBarcode.Should().Be(barcode);
        _mockHapticService.Verify(h => h.Success(), Times.Once);
    }

    [Fact]
    public void ToggleTorchCommand_FlipsTorchState()
    {
        // Arrange initial
        _sut.IsTorchOn.Should().BeFalse();

        // Act 1: Toggle on
        _sut.ToggleTorchCommand.Execute(null);

        // Assert 1
        _sut.IsTorchOn.Should().BeTrue();

        // Act 2: Toggle off
        _sut.ToggleTorchCommand.Execute(null);

        // Assert 2
        _sut.IsTorchOn.Should().BeFalse();
    }

    [Fact]
    public void ResumeScanningCommand_ClearsErrorAndEnablesScanning()
    {
        // Arrange: simulate previous failed scan
        _sut.ErrorMessage = "Previous error";
        _sut.DetectedBarcode = "OLD-BARCODE";
        _sut.IsScanning = false;

        // Act
        _sut.ResumeScanningCommand.Execute(null);

        // Assert
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.DetectedBarcode.Should().BeEmpty();
        _sut.IsScanning.Should().BeTrue();
    }

    [Fact]
    public void ResetScannerCommand_ResetsAllScanningAndErrorState()
    {
        // Arrange
        _sut.ErrorMessage = "Some error";
        _sut.DetectedBarcode = "SOME-BARCODE";
        _sut.IsBusy = true;
        _sut.IsScanning = false;

        // Act
        _sut.ResetScannerCommand.Execute(null);

        // Assert
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.DetectedBarcode.Should().BeEmpty();
        _sut.IsBusy.Should().BeFalse();
        _sut.IsScanning.Should().BeTrue();
    }

    [Fact]
    public async Task NavigateToManualEntryCommand_ExecutesWithoutThrowing()
    {
        // Act & Assert
        await _sut.NavigateToManualEntryCommand.ExecuteAsync(null);
        _sut.IsBusy.Should().BeFalse();
    }
}
