using CargoPulse.Mobile.Data.Dtos;

namespace CargoPulse.Mobile.Data.Services;

public interface IShipmentApiClient
{
    Task<ShipmentListResponseDto> GetShipmentsAsync(int page = 1, int limit = 10, string? search = null, string? status = null, CancellationToken cancellationToken = default);
    Task<ShipmentDetailResponseDto> GetShipmentByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ShipmentDetailResponseDto?> FindByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
}
