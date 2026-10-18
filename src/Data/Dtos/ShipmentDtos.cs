using System.Text.Json.Serialization;

namespace CargoPulse.Mobile.Data.Dtos;

public record ShipmentListResponseDto(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] ShipmentListDataDto Data,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);

public record ShipmentListDataDto(
    [property: JsonPropertyName("items")] List<ShipmentDto> Items,
    [property: JsonPropertyName("meta")] PaginationMetaDto Meta
);

public record PaginationMetaDto(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("totalPages")] int TotalPages
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
    [property: JsonPropertyName("alertsCount")] int? AlertsCount,
    [property: JsonPropertyName("stages")] List<FreightStageDto>? Stages,
    [property: JsonPropertyName("alerts")] List<AlertDto>? Alerts
);

public record FreightStageDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("shipmentId")] string? ShipmentId,
    [property: JsonPropertyName("stageType")] string StageType,
    [property: JsonPropertyName("stageOrder")] int StageOrder,
    [property: JsonPropertyName("contractedCost")] double? ContractedCost,
    [property: JsonPropertyName("actualCost")] double? ActualCost,
    [property: JsonPropertyName("variance")] double? Variance,
    [property: JsonPropertyName("variancePercent")] double? VariancePercent,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("auditNotes")] string? AuditNotes
);

public record AlertDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("shipmentId")] string? ShipmentId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("slaDeadline")] DateTime? SlaDeadline,
    [property: JsonPropertyName("financialImpactUsd")] double? FinancialImpactUsd,
    [property: JsonPropertyName("resolutionNotes")] string? ResolutionNotes,
    [property: JsonPropertyName("resolvedAt")] DateTime? ResolvedAt
);

public record AlertListResponseDto(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] List<AlertDto> Data,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);
