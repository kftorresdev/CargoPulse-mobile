using CargoPulse.Mobile.Core.Services;
using FluentAssertions;
using Microsoft.Maui.Networking;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class ConnectivityServiceTests
{
    private readonly Mock<IConnectivity> _mockConnectivity;

    public ConnectivityServiceTests()
    {
        _mockConnectivity = new Mock<IConnectivity>();
    }

    [Fact]
    public void IsConnected_WhenHeadlessOrNull_DefaultsToTrue()
    {
        // Arrange
        var service = new ConnectivityService(connectivity: null);

        // Act & Assert
        service.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void IsConnected_WhenNetworkAccessInternet_ReturnsTrue()
    {
        // Arrange
        _mockConnectivity.Setup(c => c.NetworkAccess).Returns(NetworkAccess.Internet);
        var service = new ConnectivityService(_mockConnectivity.Object);

        // Act & Assert
        service.IsConnected.Should().BeTrue();
    }

    [Theory]
    [InlineData(NetworkAccess.None)]
    [InlineData(NetworkAccess.Local)]
    [InlineData(NetworkAccess.ConstrainedInternet)]
    [InlineData(NetworkAccess.Unknown)]
    public void IsConnected_WhenNetworkAccessNotInternet_ReturnsFalse(NetworkAccess access)
    {
        // Arrange
        _mockConnectivity.Setup(c => c.NetworkAccess).Returns(access);
        var service = new ConnectivityService(_mockConnectivity.Object);

        // Act & Assert
        service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void IsConnected_WhenConnectivityThrowsException_DefaultsToTrueSafely()
    {
        // Arrange
        _mockConnectivity.Setup(c => c.NetworkAccess).Throws(new InvalidOperationException("Headless error"));
        var service = new ConnectivityService(_mockConnectivity.Object);

        // Act & Assert
        service.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void ConnectivityChanged_WhenMauiConnectivityFires_RaisesEventWithCorrectValue()
    {
        // Arrange
        var service = new ConnectivityService(_mockConnectivity.Object);
        bool? receivedValue = null;
        service.ConnectivityChanged += (_, isConnected) => receivedValue = isConnected;

        // Act
        _mockConnectivity.Raise(c => c.ConnectivityChanged += null, new ConnectivityChangedEventArgs(NetworkAccess.Internet, Array.Empty<ConnectionProfile>()));

        // Assert
        receivedValue.Should().BeTrue();

        // Act again for disconnect
        _mockConnectivity.Raise(c => c.ConnectivityChanged += null, new ConnectivityChangedEventArgs(NetworkAccess.None, Array.Empty<ConnectionProfile>()));

        // Assert
        receivedValue.Should().BeFalse();
    }

    [Fact]
    public void Dispose_UnsubscribesFromMauiConnectivityChanged()
    {
        // Arrange
        var service = new ConnectivityService(_mockConnectivity.Object);
        int eventCount = 0;
        service.ConnectivityChanged += (_, _) => eventCount++;

        // Act
        service.Dispose();
        _mockConnectivity.Raise(c => c.ConnectivityChanged += null, new ConnectivityChangedEventArgs(NetworkAccess.Internet, Array.Empty<ConnectionProfile>()));

        // Assert
        eventCount.Should().Be(0);
    }
}
