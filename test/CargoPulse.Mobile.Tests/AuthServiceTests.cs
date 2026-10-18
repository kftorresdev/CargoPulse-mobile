using System.Net;
using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class AuthServiceTests
{
    private readonly Mock<IAuthApiClient> _mockApiClient;
    private readonly Mock<ISecureStorageService> _mockStorage;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _mockApiClient = new Mock<IAuthApiClient>();
        _mockStorage = new Mock<ISecureStorageService>();
        _sut = new AuthService(_mockApiClient.Object, _mockStorage.Object);
    }

    [Fact]
    public async Task LoginAsync_WhenApiSucceeds_StoresTokenAndReturnsTrue()
    {
        // Arrange
        const string email = "driver@cargopulse.com";
        const string password = "SecurePassword123!";
        const string token = "sample-jwt-token-xyz";

        var loginResponse = new LoginResponseDto(
            Success: true,
            Data: new AuthDataDto(
                AccessToken: token,
                User: new UserProfileDto(
                    Id: "user-id-01",
                    Email: email,
                    FirstName: "Juan",
                    LastName: "Perez",
                    Role: "LOGISTICS_OPERATOR"
                )
            ),
            Timestamp: DateTime.UtcNow
        );

        _mockApiClient
            .Setup(c => c.LoginAsync(It.Is<LoginRequestDto>(r => r.Email == email && r.Password == password), It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await _sut.LoginAsync(email, password);

        // Assert
        result.Should().BeTrue();
        _mockStorage.Verify(s => s.SetAsync(ApiConstants.AuthTokenKey, token), Times.Once);
        _mockStorage.Verify(s => s.SetAsync(ApiConstants.UserIdKey, "user-id-01"), Times.Once);
        _mockStorage.Verify(s => s.SetAsync(ApiConstants.UserEmailKey, email), Times.Once);
        _mockStorage.Verify(s => s.SetAsync(ApiConstants.UserRoleKey, "LOGISTICS_OPERATOR"), Times.Once);
        _mockStorage.Verify(s => s.SetAsync(ApiConstants.UserFirstNameKey, "Juan"), Times.Once);
        _mockStorage.Verify(s => s.SetAsync(ApiConstants.UserLastNameKey, "Perez"), Times.Once);
        _mockStorage.Verify(s => s.SetAsync(ApiConstants.UserNameKey, "Juan Perez"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenApiThrows_ReturnsFalseAndDoesNotStoreToken()
    {
        // Arrange
        const string email = "driver@cargopulse.com";
        const string password = "WrongPassword!";

        _mockApiClient
            .Setup(c => c.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Invalid credentials", null, HttpStatusCode.Unauthorized));

        // Act
        var result = await _sut.LoginAsync(email, password);

        // Assert
        result.Should().BeFalse();
        _mockStorage.Verify(s => s.SetAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IsAuthenticatedAsync_WhenTokenExists_ReturnsTrue()
    {
        // Arrange
        _mockStorage
            .Setup(s => s.GetAsync(ApiConstants.AuthTokenKey))
            .ReturnsAsync("valid-jwt-token-12345");

        // Act
        var isAuthenticated = await _sut.IsAuthenticatedAsync();

        // Assert
        isAuthenticated.Should().BeTrue();
        _mockStorage.Verify(s => s.GetAsync(ApiConstants.AuthTokenKey), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsAuthenticatedAsync_WhenTokenEmptyOrNull_ReturnsFalse(string? emptyOrNullToken)
    {
        // Arrange
        _mockStorage
            .Setup(s => s.GetAsync(ApiConstants.AuthTokenKey))
            .ReturnsAsync(emptyOrNullToken);

        // Act
        var isAuthenticated = await _sut.IsAuthenticatedAsync();

        // Assert
        isAuthenticated.Should().BeFalse();
        _mockStorage.Verify(s => s.GetAsync(ApiConstants.AuthTokenKey), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ClearsSecureStorage()
    {
        // Act
        await _sut.LogoutAsync();

        // Assert
        _mockStorage.Verify(s => s.Remove(ApiConstants.AuthTokenKey), Times.Once);
        _mockStorage.Verify(s => s.Remove(ApiConstants.UserIdKey), Times.Once);
        _mockStorage.Verify(s => s.Remove(ApiConstants.UserEmailKey), Times.Once);
        _mockStorage.Verify(s => s.Remove(ApiConstants.UserRoleKey), Times.Once);
        _mockStorage.Verify(s => s.Remove(ApiConstants.UserFirstNameKey), Times.Once);
        _mockStorage.Verify(s => s.Remove(ApiConstants.UserLastNameKey), Times.Once);
        _mockStorage.Verify(s => s.Remove(ApiConstants.UserNameKey), Times.Once);
    }
}
