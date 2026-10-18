using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.ViewModels;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class LoginViewModelTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly LoginViewModel _sut;

    public LoginViewModelTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _sut = new LoginViewModel(_mockAuthService.Object);
    }

    [Theory]
    [InlineData("", "password123")]
    [InlineData("   ", "password123")]
    [InlineData("operator@cargopulse.com", "")]
    [InlineData("operator@cargopulse.com", "   ")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public async Task LoginCommand_WhenEmailOrPasswordEmpty_SetsErrorMessageWithoutCallingService(string? email, string? password)
    {
        // Arrange
        _sut.Email = email ?? string.Empty;
        _sut.Password = password ?? string.Empty;

        // Act
        await _sut.LoginCommand.ExecuteAsync(null);

        // Assert
        _sut.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.IsBusy.Should().BeFalse();
        _mockAuthService.Verify(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginCommand_WhenCredentialsValidAndAuthSucceeds_ClearsErrorAndCompletes()
    {
        // Arrange
        const string email = "operator@cargopulse.com";
        const string password = "ValidPassword123!";

        _sut.Email = email;
        _sut.Password = password;
        _sut.ErrorMessage = "Previous error message";

        _mockAuthService
            .Setup(s => s.LoginAsync(email, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.LoginCommand.ExecuteAsync(null);

        // Assert
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.IsBusy.Should().BeFalse();
        _mockAuthService.Verify(s => s.LoginAsync(email, password, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginCommand_WhenAuthFails_SetsErrorMessage()
    {
        // Arrange
        const string email = "operator@cargopulse.com";
        const string password = "WrongPassword!";

        _sut.Email = email;
        _sut.Password = password;

        _mockAuthService
            .Setup(s => s.LoginAsync(email, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _sut.LoginCommand.ExecuteAsync(null);

        // Assert
        _sut.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.IsBusy.Should().BeFalse();
        _mockAuthService.Verify(s => s.LoginAsync(email, password, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void TogglePasswordVisibilityCommand_FlipsFlag()
    {
        // Arrange & Assert initial state
        _sut.IsPasswordVisible.Should().BeFalse();

        // Act 1: Toggle once
        _sut.TogglePasswordVisibilityCommand.Execute(null);

        // Assert 1: Now true
        _sut.IsPasswordVisible.Should().BeTrue();

        // Act 2: Toggle second time
        _sut.TogglePasswordVisibilityCommand.Execute(null);

        // Assert 2: Back to false
        _sut.IsPasswordVisible.Should().BeFalse();
    }
}
