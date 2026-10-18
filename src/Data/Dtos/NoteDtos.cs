using System.Text.Json.Serialization;

namespace CargoPulse.Mobile.Data.Dtos;

public record CreateNoteRequestDto(
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("parentId")] string? ParentId,
    [property: JsonPropertyName("attachmentUrl")] string? AttachmentUrl
);

public record NoteAuthorDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("role")] string Role
);

public record NoteNodeDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("shipmentId")] string ShipmentId,
    [property: JsonPropertyName("authorId")] string AuthorId,
    [property: JsonPropertyName("author")] NoteAuthorDto Author,
    [property: JsonPropertyName("parentId")] string? ParentId,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("attachmentUrl")] string? AttachmentUrl,
    [property: JsonPropertyName("replies")] List<NoteNodeDto> Replies,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("updatedAt")] DateTime UpdatedAt
);

public record NoteTreeResponseDto(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] List<NoteNodeDto> Data,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);
