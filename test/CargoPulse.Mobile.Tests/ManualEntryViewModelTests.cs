using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using CargoPulse.Mobile.ViewModels;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class ManualEntryViewModelTests
{
    private readonly Mock<IShipmentApiClient> _mockShipmentApiClient;
    private readonly ManualEntryViewModel _sut;

    public ManualEntryViewModelTests()
    {
        _mockShipmentApiClient = new Mock<IShipmentApiClient>();
        _sut = new ManualEntryViewModel(_mockShipmentApiClient.Object);
    }

    [Fact]
    public void ManualEntryViewModel_InitialState_HasExpectedDefaults()
    {
        _sut.TrackingNumber.Should().BeEmpty();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.IsBusy.Should().BeFalse();
        _sut.Title.Should().Be("Búsqueda Manual");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData(" x ")]
    [InlineData(null)]
    public async Task SearchCommand_WhenTrackingEmpty_SetsErrorMessage(string? invalidTracking)
    {
        // Arrange
        _sut.TrackingNumber = invalidTracking ?? string.Empty;

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.ErrorMessage.Should().Be("Ingrese al menos 3 caracteres para buscar el tracking.");
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.IsBusy.Should().BeFalse();
        _mockShipmentApiClient.Verify(api => api.FindByTrackingNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchCommand_WhenValidAndFound_Navigates()
    {
        // Arrange
        const string trackingNumber = "CP-VALID-999";
        _sut.TrackingNumber = trackingNumber;
        _sut.ErrorMessage = "Previous error to clear";

        var shipment = new ShipmentDto(
            Id: "ship-found-999",
            TrackingNumber: trackingNumber,
            Origin: "Origin",
            Destination: "Destination",
            CurrentStatus: "IN_TRANSIT",
            CarrierName: "Carrier",
            Eta: DateTime.UtcNow,
            ActualDelivery: null,
            AlertsCount: 0,
            Stages: null,
            Alerts: null
        );

        _mockShipmentApiClient
            .Setup(api => api.FindByTrackingNumberAsync(trackingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShipmentDetailResponseDto(true, shipment, DateTime.UtcNow));

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.IsBusy.Should().BeFalse();
        _mockShipmentApiClient.Verify(api => api.FindByTrackingNumberAsync(trackingNumber, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchCommand_WhenValidAndNotFound_SetsErrorMessage()
    {
        // Arrange
        const string trackingNumber = "CP-NOT-FOUND";
        _sut.TrackingNumber = trackingNumber;

        _mockShipmentApiClient
            .Setup(api => api.FindByTrackingNumberAsync(trackingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShipmentDetailResponseDto?)null);

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.ErrorMessage.Should().Be($"No se encontró el embarque con tracking: {trackingNumber}");
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.IsBusy.Should().BeFalse();
        _mockShipmentApiClient.Verify(api => api.FindByTrackingNumberAsync(trackingNumber, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchCommand_WhenApiThrows_SetsErrorMessage()
    {
        // Arrange
        const string trackingNumber = "CP-NETWORK-ERR";
        _sut.TrackingNumber = trackingNumber;

        _mockShipmentApiClient
            .Setup(api => api.FindByTrackingNumberAsync(trackingNumber, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection lost"));

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.ErrorMessage.Should().Be($"Error al consultar el embarque: {trackingNumber}");
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task CancelCommand_ExecutesWithoutThrowing()
    {
        // Act & Assert
        await _sut.CancelCommand.ExecuteAsync(null);
        _sut.IsBusy.Should().BeFalse();
    }
}
