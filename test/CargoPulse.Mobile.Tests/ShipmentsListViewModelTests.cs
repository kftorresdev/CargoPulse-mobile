using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using CargoPulse.Mobile.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace CargoPulse.Mobile.Tests;

public class ShipmentsListViewModelTests
{
    private readonly Mock<IShipmentApiClient> _shipmentApiClientMock;
    private readonly ShipmentsListViewModel _sut;

    public ShipmentsListViewModelTests()
    {
        _shipmentApiClientMock = new Mock<IShipmentApiClient>();
        _sut = new ShipmentsListViewModel(_shipmentApiClientMock.Object);
    }

    [Fact]
    public void Constructor_WithNullShipmentApiClient_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ShipmentsListViewModel(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("shipmentApiClient");
    }

    [Fact]
    public void InitialState_ShouldHaveExpectedDefaults()
    {
        // Assert
        _sut.Shipments.Should().BeEmpty();
        _sut.SearchText.Should().BeEmpty();
        _sut.IsRefreshing.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.IsEmpty.Should().BeTrue();
        _sut.Title.Should().Be("Embarques");
    }

    [Fact]
    public async Task LoadShipmentsAsync_WhenSuccessful_PopulatesShipmentsCollection()
    {
        // Arrange
        var items = new List<ShipmentDto>
        {
            new("s1", "CP-001", "Origin 1", "Dest 1", "CUSTOMS", "Carrier A", DateTime.UtcNow, null, 0, null, null),
            new("s2", "CP-002", "Origin 2", "Dest 2", "IN_TRANSIT", "Carrier B", DateTime.UtcNow, null, 1, null, null)
        };
        var listResponse = new ShipmentListResponseDto(
            true,
            new ShipmentListDataDto(items, new PaginationMetaDto(2, 1, 50, 1)),
            DateTime.UtcNow
        );

        _shipmentApiClientMock
            .Setup(s => s.GetShipmentsAsync(1, 50, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);

        // Act
        await _sut.LoadShipmentsAsync();

        // Assert
        _sut.Shipments.Should().HaveCount(2);
        _sut.IsEmpty.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
    }

    [Fact]
    public async Task LoadShipmentsAsync_WithSearchText_PassesFilterToApi()
    {
        // Arrange
        _sut.SearchText = "CP-001";
        var items = new List<ShipmentDto>
        {
            new("s1", "CP-001", "Origin 1", "Dest 1", "CUSTOMS", "Carrier A", DateTime.UtcNow, null, 0, null, null)
        };
        var listResponse = new ShipmentListResponseDto(
            true,
            new ShipmentListDataDto(items, new PaginationMetaDto(1, 1, 50, 1)),
            DateTime.UtcNow
        );

        _shipmentApiClientMock
            .Setup(s => s.GetShipmentsAsync(1, 50, "CP-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);

        // Act
        await _sut.LoadShipmentsAsync();

        // Assert
        _sut.Shipments.Should().HaveCount(1);
        _shipmentApiClientMock.Verify(s => s.GetShipmentsAsync(1, 50, "CP-001", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadShipmentsAsync_WhenApiThrowsException_SetsErrorMessage()
    {
        // Arrange
        _shipmentApiClientMock
            .Setup(s => s.GetShipmentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Server error"));

        // Act
        await _sut.LoadShipmentsAsync();

        // Assert
        _sut.ErrorMessage.Should().Contain("Server error");
        _sut.HasErrorMessage.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_SetsIsRefreshingAndLoadsShipments()
    {
        // Arrange
        var listResponse = new ShipmentListResponseDto(
            true,
            new ShipmentListDataDto(new List<ShipmentDto>(), new PaginationMetaDto(0, 1, 50, 0)),
            DateTime.UtcNow
        );

        _shipmentApiClientMock
            .Setup(s => s.GetShipmentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);

        // Act
        await _sut.RefreshAsync();

        // Assert
        _sut.IsRefreshing.Should().BeFalse();
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task SelectShipmentAsync_WithNull_DoesNothing()
    {
        // Act
        await _sut.SelectShipmentAsync(null);

        // Assert - Should not throw
        _sut.Shipments.Should().BeEmpty();
    }
}
