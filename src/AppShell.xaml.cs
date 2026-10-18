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
        Routing.RegisterRoute("sync", typeof(SyncQueuePage));
    }
}
