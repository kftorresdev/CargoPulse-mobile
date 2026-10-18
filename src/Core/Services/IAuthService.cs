using CargoPulse.Mobile.Data.Dtos;

namespace CargoPulse.Mobile.Core.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetAccessTokenAsync();
    Task<UserProfileDto?> GetCurrentUserAsync();
    Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task LogoutAsync();
}
