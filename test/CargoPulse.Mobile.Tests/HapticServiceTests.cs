using CargoPulse.Mobile.Core.Services;
using FluentAssertions;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class HapticServiceTests
{
    [Fact]
    public void Click_CallsPerformWithClick()
    {
        // Arrange
        var mockHaptic = new Mock<IHapticFeedback>();
        var service = new HapticService(mockHaptic.Object);

        // Act
        service.Click();

        // Assert
        mockHaptic.Verify(h => h.Perform(HapticFeedbackType.Click), Times.Once);
    }

    [Fact]
    public void Click_WhenNotSupportedExceptionThrown_DoesNotRethrow()
    {
        // Arrange
        var mockHaptic = new Mock<IHapticFeedback>();
        mockHaptic.Setup(h => h.Perform(HapticFeedbackType.Click)).Throws(new NotSupportedException("Device lacks haptic motor"));
        var service = new HapticService(mockHaptic.Object);

        // Act & Assert
        var act = () => service.Click();
        act.Should().NotThrow();
    }

    [Fact]
    public void Click_WhenFeatureNotSupportedExceptionThrown_DoesNotRethrow()
    {
        // Arrange
        var mockHaptic = new Mock<IHapticFeedback>();
        mockHaptic.Setup(h => h.Perform(HapticFeedbackType.Click)).Throws(new FeatureNotSupportedException("Haptics not supported"));
        var service = new HapticService(mockHaptic.Object);

        // Act & Assert
        var act = () => service.Click();
        act.Should().NotThrow();
    }

    [Fact]
    public void Success_CallsPerformWithLongPress()
    {
        // Arrange
        var mockHaptic = new Mock<IHapticFeedback>();
        var service = new HapticService(mockHaptic.Object);

        // Act
        service.Success();

        // Assert
        mockHaptic.Verify(h => h.Perform(HapticFeedbackType.LongPress), Times.Once);
    }

    [Fact]
    public void Success_WhenNotSupportedExceptionThrown_DoesNotRethrow()
    {
        // Arrange
        var mockHaptic = new Mock<IHapticFeedback>();
        mockHaptic.Setup(h => h.Perform(HapticFeedbackType.LongPress)).Throws(new NotSupportedException("Device lacks haptic motor"));
        var service = new HapticService(mockHaptic.Object);

        // Act & Assert
        var act = () => service.Success();
        act.Should().NotThrow();
    }

    [Fact]
    public void Success_WhenFeatureNotSupportedExceptionThrown_DoesNotRethrow()
    {
        // Arrange
        var mockHaptic = new Mock<IHapticFeedback>();
        mockHaptic.Setup(h => h.Perform(HapticFeedbackType.LongPress)).Throws(new FeatureNotSupportedException("Haptics not supported"));
        var service = new HapticService(mockHaptic.Object);

        // Act & Assert
        var act = () => service.Success();
        act.Should().NotThrow();
    }
}
