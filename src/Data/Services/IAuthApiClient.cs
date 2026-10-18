using CargoPulse.Mobile.Data.Dtos;

namespace CargoPulse.Mobile.Data.Services;

public interface IAuthApiClient
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
