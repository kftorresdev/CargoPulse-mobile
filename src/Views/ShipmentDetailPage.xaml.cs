using CargoPulse.Mobile.ViewModels;

namespace CargoPulse.Mobile.Views;

public partial class ShipmentDetailPage : ContentPage
{
    public ShipmentDetailPage(ShipmentDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
