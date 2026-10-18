using SQLite;

namespace CargoPulse.Mobile.Data.Local.Entities;

[Table("Inspections")]
public class LocalInspectionRecord
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    public string ShipmentId { get; set; } = string.Empty;

    public string TrackingNumber { get; set; } = string.Empty;

    public string ContainerCondition { get; set; } = "GOOD";

    public bool SealIntact { get; set; } = true;

    public string DeclaredSealNumber { get; set; } = string.Empty;

    public string ActualSealNumber { get; set; } = string.Empty;

    public bool SealMismatch { get; set; }

    public bool DamagesReported { get; set; }

    public string? DamagesDescription { get; set; }

    public string PhotosJson { get; set; } = "[]";

    public string SignatureBase64 { get; set; } = string.Empty;

    public string SignerName { get; set; } = string.Empty;

    public string? GpsCoordinates { get; set; }

    [Indexed]
    public string SyncStatus { get; set; } = "PENDING"; // "PENDING", "SYNCED", "FAILED"

    public int RetryCount { get; set; }

    public string? LastError { get; set; }

    public DateTime InspectedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SyncedAt { get; set; }

    [Indexed]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
