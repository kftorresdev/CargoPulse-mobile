using System.Text.Json.Serialization;

namespace CargoPulse.Mobile.Data.Dtos;

public record ShipmentListResponseDto(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] List<ShipmentDto> Data,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);

public record ShipmentDetailResponseDto(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] ShipmentDto Data,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);

public record ShipmentDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("trackingNumber")] string TrackingNumber,
    [property: JsonPropertyName("origin")] string Origin,
    [property: JsonPropertyName("destination")] string Destination,
    [property: JsonPropertyName("currentStatus")] string CurrentStatus,
    [property: JsonPropertyName("carrierName")] string CarrierName,
    [property: JsonPropertyName("eta")] DateTime Eta,
    [property: JsonPropertyName("actualDelivery")] DateTime? ActualDelivery,
    [property: JsonPropertyName("stages")] List<FreightStageDto>? Stages,
    [property: JsonPropertyName("alerts")] List<AlertDto>? Alerts
);

public record FreightStageDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("stageType")] string StageType,
    [property: JsonPropertyName("stageOrder")] int StageOrder,
    [property: JsonPropertyName("status")] string Status
);

public record AlertDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("status")] string Status
);
