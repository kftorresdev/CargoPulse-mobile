using System.Globalization;
using CargoPulse.Mobile.Core.Services;
using FluentAssertions;
using Microsoft.Maui.Devices.Sensors;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class GeolocationServiceTests
{
    private readonly Mock<IGeolocation> _mockGeolocation;
    private readonly GeolocationService _service;

    public GeolocationServiceTests()
    {
        _mockGeolocation = new Mock<IGeolocation>();
        _service = new GeolocationService(_mockGeolocation.Object);
    }

    [Fact]
    public async Task GetCurrentCoordinatesAsync_WhenLocationFound_ReturnsFormattedCoordinates()
    {
        // Arrange
        var location = new Location(19.432612, -99.133245);
        _mockGeolocation
            .Setup(g => g.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        // Act
        var result = await _service.GetCurrentCoordinatesAsync();

        // Assert
        result.Should().Be("19.4326,-99.1332");
    }

    [Fact]
    public async Task GetCurrentCoordinatesAsync_WhenLocationNull_ReturnsNull()
    {
        // Arrange
        _mockGeolocation
            .Setup(g => g.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        // Act
        var result = await _service.GetCurrentCoordinatesAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentCoordinatesAsync_WhenPermissionException_ReturnsNull()
    {
        // Arrange
        _mockGeolocation
            .Setup(g => g.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PermissionException("Location permission denied"));

        // Act
        var result = await _service.GetCurrentCoordinatesAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentCoordinatesAsync_WhenTimeoutException_ReturnsNull()
    {
        // Arrange
        _mockGeolocation
            .Setup(g => g.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Location request timed out"));

        // Act
        var result = await _service.GetCurrentCoordinatesAsync();

        // Assert
        result.Should().BeNull();
    }
}
