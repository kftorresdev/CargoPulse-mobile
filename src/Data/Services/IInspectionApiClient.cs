using CargoPulse.Mobile.Data.Dtos;

namespace CargoPulse.Mobile.Data.Services;

public interface IInspectionApiClient
{
    Task<InspectionSyncResponseDto> UploadInspectionAsync(CreateInspectionRequestDto dto, CancellationToken cancellationToken = default);
}
