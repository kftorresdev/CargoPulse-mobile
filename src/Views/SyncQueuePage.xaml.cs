using CargoPulse.Mobile.ViewModels;

namespace CargoPulse.Mobile.Views;

public partial class SyncQueuePage : ContentPage
{
    private readonly SyncQueueViewModel _viewModel;

    public SyncQueuePage(SyncQueueViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshCommand.Execute(null);
    }
}
