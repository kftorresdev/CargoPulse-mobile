using CargoPulse.Mobile.ViewModels;

namespace CargoPulse.Mobile.Views;

public partial class ManualEntryPage : ContentPage
{
    public ManualEntryPage(ManualEntryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
