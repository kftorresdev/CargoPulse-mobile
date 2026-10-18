using System.Text.Encodings.Web;
using System.Text.Json;
using CargoPulse.Mobile.Data.Dtos;
using FluentAssertions;

namespace CargoPulse.Mobile.Tests;

public class DtoSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [Fact]
    public void LoginRequestDto_SerializesWithCorrectPropertyNames()
    {
        // Arrange
        var dto = new LoginRequestDto("inspector@cargopulse.com", "Secret123!");

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"email\":\"inspector@cargopulse.com\"");
        json.Should().Contain("\"password\":\"Secret123!\"");
    }

    [Fact]
    public void LoginResponseDto_DeserializesProperly()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "accessToken": "jwt_token_sample",
                "user": {
                    "id": "u-123",
                    "email": "test@cargopulse.com",
                    "firstName": "Fernando",
                    "lastName": "Torres",
                    "role": "LOGISTICS_OPERATOR"
                }
            },
            "timestamp": "2026-10-17T20:00:00Z"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<LoginResponseDto>(json, JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.AccessToken.Should().Be("jwt_token_sample");
        result.Data.User.Role.Should().Be("LOGISTICS_OPERATOR");
        result.Data.User.FirstName.Should().Be("Fernando");
    }

    [Fact]
    public void CreateInspectionRequestDto_SerializesAndRoundTripsWithoutErrors()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dto = new CreateInspectionRequestDto(
            Id: Guid.NewGuid().ToString(),
            ShipmentId: Guid.NewGuid().ToString(),
            ContainerCondition: "GOOD",
            SealIntact: true,
            SealNumberDeclared: "SEAL-1234",
            SealNumberActual: "SEAL-1234",
            SealMismatch: false,
            DamagesReported: false,
            DamagesDescription: null,
            Photos: new List<PhotoUploadDto>(),
            SignatureBase64: "data:image/png;base64,iVBORw0KGgo...",
            SignerName: "Juan Pérez",
            GpsCoordinates: "27.4861,-99.5085",
            InspectedAt: now
        );

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateInspectionRequestDto>(json, JsonOptions);

        // Assert
        json.Should().Contain("\"containerCondition\":\"GOOD\"");
        json.Should().Contain("\"sealIntact\":true");
        json.Should().Contain("\"signerName\":\"Juan Pérez\"");

        deserialized.Should().NotBeNull();
        deserialized!.SignerName.Should().Be("Juan Pérez");
        deserialized.SealIntact.Should().BeTrue();
        deserialized.ContainerCondition.Should().Be("GOOD");
    }
}
