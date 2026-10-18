using System.Net;
using System.Text;
using System.Text.Json;
using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Core.Handlers;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace CargoPulse.Mobile.Tests;

public class AuthServicesTests
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

    #region AuthApiClient Tests

    [Fact]
    public async Task AuthApiClient_LoginAsync_WhenSuccessful_ReturnsLoginResponseDto()
    {
        // Arrange
        var expectedResponse = new LoginResponseDto(
            Success: true,
            Data: new AuthDataDto(
                AccessToken: "test-token-123",
                User: new UserProfileDto(
                    Id: "user-1",
                    Email: "test@example.com",
                    FirstName: "Carlos",
                    LastName: "Gomez",
                    Role: "LOGISTICS_OPERATOR"
                )
            ),
            Timestamp: DateTime.UtcNow
        );

        var handler = new FakeHttpMessageHandler(request =>
        {
            request.RequestUri!.ToString().Should().EndWith(ApiConstants.LoginEndpoint);
            request.Method.Should().Be(HttpMethod.Post);

            var json = JsonSerializer.Serialize(expectedResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var apiClient = new AuthApiClient(httpClient);

        // Act
        var result = await apiClient.LoginAsync(new LoginRequestDto("test@example.com", "Password123!"));

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.AccessToken.Should().Be("test-token-123");
        result.Data.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task AuthApiClient_LoginAsync_WhenUnauthorized_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":\"Invalid credentials\"}", Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000/api/") };
        var apiClient = new AuthApiClient(httpClient);

        // Act
        var act = async () => await apiClient.LoginAsync(new LoginRequestDto("wrong@example.com", "bad-pass"));

        // Assert
        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Which.Message.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task AuthApiClient_LoginAsync_WhenNullRequest_ThrowsArgumentNullException()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var apiClient = new AuthApiClient(httpClient);

        var act = async () => await apiClient.LoginAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region AuthService Tests

    [Fact]
    public async Task AuthService_LoginAsync_WhenSuccessful_PersistsUserDataAndReturnsTrue()
    {
        // Arrange
        var mockApiClient = new Mock<IAuthApiClient>();
        var mockStorage = new Mock<ISecureStorageService>();

        var loginResponse = new LoginResponseDto(
            Success: true,
            Data: new AuthDataDto(
                AccessToken: "secure-jwt-token",
                User: new UserProfileDto(
                    Id: "user-42",
                    Email: "driver@cargopulse.com",
                    FirstName: "Juan",
                    LastName: "Perez",
                    Role: "INSPECTOR"
                )
            ),
            Timestamp: DateTime.UtcNow
        );

        mockApiClient
            .Setup(c => c.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginResponse);

        var authService = new AuthService(mockApiClient.Object, mockStorage.Object);

        // Act
        var result = await authService.LoginAsync("driver@cargopulse.com", "SecretPass");

        // Assert
        result.Should().BeTrue();

        mockStorage.Verify(s => s.SetAsync(ApiConstants.AuthTokenKey, "secure-jwt-token"), Times.Once);
        mockStorage.Verify(s => s.SetAsync(ApiConstants.UserIdKey, "user-42"), Times.Once);
        mockStorage.Verify(s => s.SetAsync(ApiConstants.UserEmailKey, "driver@cargopulse.com"), Times.Once);
        mockStorage.Verify(s => s.SetAsync(ApiConstants.UserRoleKey, "INSPECTOR"), Times.Once);
        mockStorage.Verify(s => s.SetAsync(ApiConstants.UserFirstNameKey, "Juan"), Times.Once);
        mockStorage.Verify(s => s.SetAsync(ApiConstants.UserLastNameKey, "Perez"), Times.Once);
        mockStorage.Verify(s => s.SetAsync(ApiConstants.UserNameKey, "Juan Perez"), Times.Once);
    }

    [Fact]
    public async Task AuthService_LoginAsync_WhenEmptyEmailOrPassword_ThrowsArgumentException()
    {
        var authService = new AuthService(Mock.Of<IAuthApiClient>(), Mock.Of<ISecureStorageService>());

        var actEmptyEmail = async () => await authService.LoginAsync("", "pass");
        var actEmptyPass = async () => await authService.LoginAsync("email@test.com", "");

        await actEmptyEmail.Should().ThrowAsync<ArgumentException>();
        await actEmptyPass.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AuthService_IsAuthenticatedAsync_WhenTokenExists_ReturnsTrue()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey)).ReturnsAsync("valid-token");

        var authService = new AuthService(Mock.Of<IAuthApiClient>(), mockStorage.Object);

        // Act
        var isAuth = await authService.IsAuthenticatedAsync();

        // Assert
        isAuth.Should().BeTrue();
    }

    [Fact]
    public async Task AuthService_IsAuthenticatedAsync_WhenTokenMissingOrWhitespace_ReturnsFalse()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey)).ReturnsAsync("   ");

        var authService = new AuthService(Mock.Of<IAuthApiClient>(), mockStorage.Object);

        // Act
        var isAuth = await authService.IsAuthenticatedAsync();

        // Assert
        isAuth.Should().BeFalse();
    }

    [Fact]
    public async Task AuthService_GetAccessTokenAsync_ReturnsStoredToken()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey)).ReturnsAsync("token-abc");

        var authService = new AuthService(Mock.Of<IAuthApiClient>(), mockStorage.Object);

        // Act
        var token = await authService.GetAccessTokenAsync();

        // Assert
        token.Should().Be("token-abc");
    }

    [Fact]
    public async Task AuthService_GetCurrentUserAsync_ConstructsUserProfileDtoFromStorage()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey)).ReturnsAsync("valid-token");
        mockStorage.Setup(s => s.GetAsync(ApiConstants.UserIdKey)).ReturnsAsync("usr-100");
        mockStorage.Setup(s => s.GetAsync(ApiConstants.UserEmailKey)).ReturnsAsync("op@cargo.com");
        mockStorage.Setup(s => s.GetAsync(ApiConstants.UserRoleKey)).ReturnsAsync("OPERATOR");
        mockStorage.Setup(s => s.GetAsync(ApiConstants.UserFirstNameKey)).ReturnsAsync("Maria");
        mockStorage.Setup(s => s.GetAsync(ApiConstants.UserLastNameKey)).ReturnsAsync("Lopez");

        var authService = new AuthService(Mock.Of<IAuthApiClient>(), mockStorage.Object);

        // Act
        var profile = await authService.GetCurrentUserAsync();

        // Assert
        profile.Should().NotBeNull();
        profile!.Id.Should().Be("usr-100");
        profile.Email.Should().Be("op@cargo.com");
        profile.Role.Should().Be("OPERATOR");
        profile.FirstName.Should().Be("Maria");
        profile.LastName.Should().Be("Lopez");
    }

    [Fact]
    public async Task AuthService_GetCurrentUserAsync_WhenTokenMissing_ReturnsNull()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey)).ReturnsAsync((string?)null);

        var authService = new AuthService(Mock.Of<IAuthApiClient>(), mockStorage.Object);

        // Act
        var profile = await authService.GetCurrentUserAsync();

        // Assert
        profile.Should().BeNull();
    }

    [Fact]
    public async Task AuthService_LogoutAsync_RemovesKeysFromStorage()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        var authService = new AuthService(Mock.Of<IAuthApiClient>(), mockStorage.Object);

        // Act
        await authService.LogoutAsync();

        // Assert
        mockStorage.Verify(s => s.Remove(ApiConstants.AuthTokenKey), Times.Once);
        mockStorage.Verify(s => s.Remove(ApiConstants.UserIdKey), Times.Once);
        mockStorage.Verify(s => s.Remove(ApiConstants.UserEmailKey), Times.Once);
        mockStorage.Verify(s => s.Remove(ApiConstants.UserRoleKey), Times.Once);
        mockStorage.Verify(s => s.Remove(ApiConstants.UserFirstNameKey), Times.Once);
        mockStorage.Verify(s => s.Remove(ApiConstants.UserLastNameKey), Times.Once);
        mockStorage.Verify(s => s.Remove(ApiConstants.UserNameKey), Times.Once);
    }

    #endregion

    #region AuthenticatedHttpClientHandler Tests

    [Fact]
    public async Task AuthenticatedHttpClientHandler_AddsBearerToken_WhenTokenExists()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey)).ReturnsAsync("my-jwt-token");

        HttpRequestMessage? interceptedRequest = null;
        var innerHandler = new FakeHttpMessageHandler(req =>
        {
            interceptedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler = new AuthenticatedHttpClientHandler(mockStorage.Object, innerHandler);
        var invoker = new HttpMessageInvoker(handler);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:3000/api/shipments");
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        interceptedRequest.Should().NotBeNull();
        interceptedRequest!.Headers.Authorization.Should().NotBeNull();
        interceptedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        interceptedRequest.Headers.Authorization!.Parameter.Should().Be("my-jwt-token");
    }

    [Fact]
    public async Task AuthenticatedHttpClientHandler_OmitsAuthorization_WhenTokenMissing()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey)).ReturnsAsync((string?)null);

        HttpRequestMessage? interceptedRequest = null;
        var innerHandler = new FakeHttpMessageHandler(req =>
        {
            interceptedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler = new AuthenticatedHttpClientHandler(mockStorage.Object, innerHandler);
        var invoker = new HttpMessageInvoker(handler);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:3000/api/shipments");
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        interceptedRequest.Should().NotBeNull();
        interceptedRequest!.Headers.Authorization.Should().BeNull();
    }

    #endregion
}
