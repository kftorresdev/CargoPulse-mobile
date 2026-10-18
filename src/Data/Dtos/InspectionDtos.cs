using System.Text.Json.Serialization;

namespace CargoPulse.Mobile.Data.Dtos;

public record CreateInspectionRequestDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("shipmentId")] string ShipmentId,
    [property: JsonPropertyName("containerCondition")] string ContainerCondition,
    [property: JsonPropertyName("sealIntact")] bool SealIntact,
    [property: JsonPropertyName("sealNumberDeclared")] string SealNumberDeclared,
    [property: JsonPropertyName("sealNumberActual")] string SealNumberActual,
    [property: JsonPropertyName("sealMismatch")] bool SealMismatch,
    [property: JsonPropertyName("damagesReported")] bool DamagesReported,
    [property: JsonPropertyName("damagesDescription")] string? DamagesDescription,
    [property: JsonPropertyName("photos")] List<PhotoUploadDto> Photos,
    [property: JsonPropertyName("signatureBase64")] string SignatureBase64,
    [property: JsonPropertyName("signerName")] string SignerName,
    [property: JsonPropertyName("gpsCoordinates")] string? GpsCoordinates,
    [property: JsonPropertyName("inspectedAt")] DateTime InspectedAt
);

public record PhotoUploadDto(
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("base64Content")] string Base64Content
);

public record InspectionSyncResponseDto(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] InspectionSyncResultDto Data,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);

public record InspectionSyncResultDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("shipmentId")] string ShipmentId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("syncedAt")] DateTime SyncedAt,
    [property: JsonPropertyName("alertCreated")] bool AlertCreated
);
