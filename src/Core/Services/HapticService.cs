using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace CargoPulse.Mobile.Core.Services;

public class HapticService : IHapticService
{
    private readonly IHapticFeedback _hapticFeedback;

    public HapticService(IHapticFeedback? hapticFeedback = null)
    {
        _hapticFeedback = hapticFeedback ?? HapticFeedback.Default;
    }

    public void Click()
    {
        try
        {
            _hapticFeedback.Perform(HapticFeedbackType.Click);
        }
        catch (FeatureNotSupportedException)
        {
            // Ignored on platforms without haptic support
        }
        catch (NotSupportedException)
        {
            // Ignored on devices/emulators without haptic support
        }
        catch (Exception)
        {
            // Safe fallback to prevent unexpected crashes
        }
    }

    public void Success()
    {
        try
        {
            _hapticFeedback.Perform(HapticFeedbackType.LongPress);
        }
        catch (FeatureNotSupportedException)
        {
            // Ignored on platforms without haptic support
        }
        catch (NotSupportedException)
        {
            // Ignored on devices/emulators without haptic support
        }
        catch (Exception)
        {
            // Safe fallback to prevent unexpected crashes
        }
    }
}
