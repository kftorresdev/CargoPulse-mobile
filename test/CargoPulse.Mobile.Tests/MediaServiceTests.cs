using CargoPulse.Mobile.Core.Services;
using FluentAssertions;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class MediaServiceTests
{
    private readonly Mock<IMediaPicker> _mockMediaPicker;
    private readonly MediaService _service;

    public MediaServiceTests()
    {
        _mockMediaPicker = new Mock<IMediaPicker>();
        _service = new MediaService(_mockMediaPicker.Object);
    }

    [Fact]
    public async Task CapturePhotoAsync_WhenUserCancels_ReturnsNull()
    {
        // Arrange
        _mockMediaPicker
            .Setup(m => m.CapturePhotoAsync(It.IsAny<MediaPickerOptions?>()))
            .ReturnsAsync((FileResult?)null);

        // Act
        var result = await _service.CapturePhotoAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CapturePhotoAsync_WhenPermissionDenied_ReturnsNull()
    {
        // Arrange
        _mockMediaPicker
            .Setup(m => m.CapturePhotoAsync(It.IsAny<MediaPickerOptions?>()))
            .ThrowsAsync(new PermissionException("Camera permission denied"));

        // Act
        var result = await _service.CapturePhotoAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CapturePhotoAsync_WhenFeatureNotSupported_ReturnsNull()
    {
        // Arrange
        _mockMediaPicker
            .Setup(m => m.CapturePhotoAsync(It.IsAny<MediaPickerOptions?>()))
            .ThrowsAsync(new FeatureNotSupportedException("Camera not supported"));

        // Act
        var result = await _service.CapturePhotoAsync();

        // Assert
        result.Should().BeNull();
    }
}
