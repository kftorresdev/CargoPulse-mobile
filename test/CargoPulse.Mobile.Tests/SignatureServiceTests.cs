using CargoPulse.Mobile.Core.Services;
using FluentAssertions;

namespace CargoPulse.Mobile.Tests;

public class SignatureServiceTests
{
    private readonly SignatureService _service;

    public SignatureServiceTests()
    {
        _service = new SignatureService();
    }

    [Fact]
    public async Task ConvertDrawingStreamToBase64Async_WhenStreamNull_ReturnsEmptyString()
    {
        // Act
        var result = await _service.ConvertDrawingStreamToBase64Async(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ConvertDrawingStreamToBase64Async_WhenStreamEmpty_ReturnsEmptyString()
    {
        // Arrange
        using var emptyStream = new MemoryStream();

        // Act
        var result = await _service.ConvertDrawingStreamToBase64Async(emptyStream);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ConvertDrawingStreamToBase64Async_WhenValidStream_ReturnsDataUriBase64()
    {
        // Arrange
        var testBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 10, 20, 30, 40 };
        using var validStream = new MemoryStream(testBytes);
        var expectedBase64 = $"data:image/png;base64,{Convert.ToBase64String(testBytes)}";

        // Act
        var result = await _service.ConvertDrawingStreamToBase64Async(validStream);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Be(expectedBase64);
        result.Should().StartWith("data:image/png;base64,");
    }
}
