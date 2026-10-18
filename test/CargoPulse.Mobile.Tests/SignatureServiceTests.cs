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

    [Fact]
    public async Task ConvertDrawingStreamToBase64Async_WhenCorruptOrNonImageBytes_ReturnsEmptyString()
    {
        // Arrange - random non-image bytes
        var corruptBytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        using var corruptStream = new MemoryStream(corruptBytes);

        // Act
        var result = await _service.ConvertDrawingStreamToBase64Async(corruptStream);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ConvertDrawingStreamToBase64Async_WhenFewerThanFourBytes_ReturnsEmptyString()
    {
        // Arrange
        var tinyBytes = new byte[] { 0x89, 0x50 };
        using var tinyStream = new MemoryStream(tinyBytes);

        // Act
        var result = await _service.ConvertDrawingStreamToBase64Async(tinyStream);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ConvertDrawingStreamToBase64Async_WhenStreamExceedsMaxSize_ReturnsEmptyString()
    {
        // Arrange - Create a dummy stream larger than 5 MB
        // We use a stream that generates bytes on the fly without large allocation
        var oversizedStream = new MemoryStream(new byte[6 * 1024 * 1024]);

        // Act
        var result = await _service.ConvertDrawingStreamToBase64Async(oversizedStream);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ConvertDrawingStreamToBase64Async_WhenValidJpegStream_ReturnsDataUriBase64()
    {
        // Arrange - JPEG header: FF D8 FF
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        using var validJpegStream = new MemoryStream(jpegBytes);

        // Act
        var result = await _service.ConvertDrawingStreamToBase64Async(validJpegStream);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().StartWith("data:image/png;base64,");
    }
}

