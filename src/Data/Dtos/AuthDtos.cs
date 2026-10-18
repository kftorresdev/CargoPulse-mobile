using System.Text.Json.Serialization;

namespace CargoPulse.Mobile.Data.Dtos;

public record LoginRequestDto(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password
);

public record LoginResponseDto(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] AuthDataDto Data,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);

public record AuthDataDto(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("user")] UserProfileDto User
);

public record UserProfileDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("role")] string Role
);
