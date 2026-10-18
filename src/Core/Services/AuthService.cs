using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;

namespace CargoPulse.Mobile.Core.Services;

public class AuthService : IAuthService
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ISecureStorageService _secureStorageService;

    public AuthService(IAuthApiClient authApiClient, ISecureStorageService secureStorageService)
    {
        _authApiClient = authApiClient ?? throw new ArgumentNullException(nameof(authApiClient));
        _secureStorageService = secureStorageService ?? throw new ArgumentNullException(nameof(secureStorageService));
    }

    public string? LastErrorMessage { get; private set; }

    public async Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        LastErrorMessage = null;
        var request = new LoginRequestDto(email, password);
        LoginResponseDto? response;
        try
        {
            response = await _authApiClient.LoginAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LastErrorMessage = ex.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Credenciales incorrectas. Verifique su correo y contraseña."
                : $"Error de conexión con {ApiConstants.BaseUrl} ({ex.StatusCode?.ToString() ?? "Red"}): {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            LastErrorMessage = $"Error inesperado al conectar: {ex.Message}";
            return false;
        }

        if (response?.Success == true && response.Data?.User != null && !string.IsNullOrWhiteSpace(response.Data.AccessToken))
        {
            var data = response.Data;
            var user = data.User;

            await _secureStorageService.SetAsync(ApiConstants.AuthTokenKey, data.AccessToken);
            await _secureStorageService.SetAsync(ApiConstants.UserIdKey, user.Id ?? string.Empty);
            await _secureStorageService.SetAsync(ApiConstants.UserEmailKey, user.Email ?? string.Empty);
            await _secureStorageService.SetAsync(ApiConstants.UserRoleKey, user.Role ?? string.Empty);
            await _secureStorageService.SetAsync(ApiConstants.UserFirstNameKey, user.FirstName ?? string.Empty);
            await _secureStorageService.SetAsync(ApiConstants.UserLastNameKey, user.LastName ?? string.Empty);
            await _secureStorageService.SetAsync(ApiConstants.UserNameKey, $"{user.FirstName} {user.LastName}".Trim());

            return true;
        }

        LastErrorMessage = "La respuesta del servidor no contiene credenciales válidas.";
        return false;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _secureStorageService.GetAsync(ApiConstants.AuthTokenKey);
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _secureStorageService.GetAsync(ApiConstants.AuthTokenKey);
    }

    public async Task<UserProfileDto?> GetCurrentUserAsync()
    {
        var token = await _secureStorageService.GetAsync(ApiConstants.AuthTokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var id = await _secureStorageService.GetAsync(ApiConstants.UserIdKey);
        var email = await _secureStorageService.GetAsync(ApiConstants.UserEmailKey);
        var role = await _secureStorageService.GetAsync(ApiConstants.UserRoleKey);
        var firstName = await _secureStorageService.GetAsync(ApiConstants.UserFirstNameKey);
        var lastName = await _secureStorageService.GetAsync(ApiConstants.UserLastNameKey);

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return new UserProfileDto(
            Id: id,
            Email: email,
            FirstName: firstName ?? string.Empty,
            LastName: lastName ?? string.Empty,
            Role: role ?? string.Empty
        );
    }

    public Task LogoutAsync()
    {
        _secureStorageService.Remove(ApiConstants.AuthTokenKey);
        _secureStorageService.Remove(ApiConstants.UserIdKey);
        _secureStorageService.Remove(ApiConstants.UserEmailKey);
        _secureStorageService.Remove(ApiConstants.UserRoleKey);
        _secureStorageService.Remove(ApiConstants.UserFirstNameKey);
        _secureStorageService.Remove(ApiConstants.UserLastNameKey);
        _secureStorageService.Remove(ApiConstants.UserNameKey);

        return Task.CompletedTask;
    }
}
