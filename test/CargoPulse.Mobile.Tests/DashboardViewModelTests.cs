using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.ViewModels;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class DashboardViewModelTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly DashboardViewModel _sut;

    public DashboardViewModelTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _sut = new DashboardViewModel(_mockAuthService.Object);
    }

    [Fact]
    public void DashboardViewModel_InitialState_HasExpectedDefaults()
    {
        _sut.OperatorName.Should().BeEmpty();
        _sut.OperatorRole.Should().BeEmpty();
        _sut.CurrentDate.Should().BeEmpty();
        _sut.PendingSyncCount.Should().Be(0);
        _sut.IsBusy.Should().BeFalse();
        _sut.Title.Should().Be("Dashboard");
    }

    [Fact]
    public async Task InitializeAsync_WhenUserExists_PopulatesProperties()
    {
        // Arrange
        var testUser = new UserProfileDto(
            Id: "usr-123",
            Email: "op.patio@cargopulse.com",
            FirstName: "Carlos",
            LastName: "Mendoza",
            Role: "LOGISTICS_OPERATOR"
        );

        _mockAuthService
            .Setup(s => s.GetCurrentUserAsync())
            .ReturnsAsync(testUser);

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.OperatorName.Should().Be("Carlos Mendoza");
        _sut.OperatorRole.Should().Be("LOGISTICS_OPERATOR");
        _sut.CurrentDate.Should().NotBeNullOrWhiteSpace();
        _sut.IsBusy.Should().BeFalse();
        _mockAuthService.Verify(s => s.GetCurrentUserAsync(), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenUserNameEmpty_FallsBackToEmail()
    {
        // Arrange
        var testUser = new UserProfileDto(
            Id: "usr-456",
            Email: "chofer@cargopulse.com",
            FirstName: "",
            LastName: "",
            Role: "LOGISTICS_OPERATOR"
        );

        _mockAuthService
            .Setup(s => s.GetCurrentUserAsync())
            .ReturnsAsync(testUser);

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.OperatorName.Should().Be("chofer@cargopulse.com");
        _sut.OperatorRole.Should().Be("LOGISTICS_OPERATOR");
    }

    [Fact]
    public async Task InitializeAsync_WhenUserNull_ProvidesDefaults()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.GetCurrentUserAsync())
            .ReturnsAsync((UserProfileDto?)null);

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.OperatorName.Should().Be("Operador");
        _sut.OperatorRole.Should().Be("LOGISTICS_OPERATOR");
        _sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_RecoversGracefully()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.GetCurrentUserAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.OperatorName.Should().Be("Operador");
        _sut.OperatorRole.Should().Be("LOGISTICS_OPERATOR");
        _sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutCommand_CallsAuthServiceLogout()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.LogoutAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.LogoutCommand.ExecuteAsync(null);

        // Assert
        _mockAuthService.Verify(s => s.LogoutAsync(), Times.Once);
        _sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task NavigationCommands_ExecuteWithoutThrowingWhenShellCurrentIsNull()
    {
        // Act & Assert (in test runner Shell.Current is null)
        await _sut.NavigateToScannerCommand.ExecuteAsync(null);
        await _sut.NavigateToSyncQueueCommand.ExecuteAsync(null);
        await _sut.NavigateToShipmentsCommand.ExecuteAsync(null);

        // No exceptions thrown
        _sut.IsBusy.Should().BeFalse();
    }
}
