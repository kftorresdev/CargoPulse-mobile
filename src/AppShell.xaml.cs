using CargoPulse.Mobile.Views;

namespace CargoPulse.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("shipment/detail", typeof(ShipmentDetailPage));
        Routing.RegisterRoute("shipment/inspect", typeof(InspectionPage));
        Routing.RegisterRoute("scanner/manual", typeof(ManualEntryPage));
    }

    protected override async void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        if (args.Target?.Location != null)
        {
            var route = args.Target.Location.OriginalString;
            if (route.Equals("//main/scanner", StringComparison.OrdinalIgnoreCase) ||
                route.EndsWith("/scanner", StringComparison.OrdinalIgnoreCase))
            {
                var scannerTab = CurrentItem?.Items?.FirstOrDefault(t => t.Route == "scanner");
                if (scannerTab != null && scannerTab.Navigation.NavigationStack.Count > 1)
                {
                    try
                    {
                        await scannerTab.Navigation.PopToRootAsync(false);
                    }
                    catch
                    {
                        // Ignore if navigation is already in progress or already popped
                    }
                }
            }
        }
    }
}
