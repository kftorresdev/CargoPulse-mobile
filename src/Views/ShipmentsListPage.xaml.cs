using CargoPulse.Mobile.ViewModels;

namespace CargoPulse.Mobile.Views;

public partial class ShipmentsListPage : ContentPage
{
    public ShipmentsListPage(ShipmentsListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
