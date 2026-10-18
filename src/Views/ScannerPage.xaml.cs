using CargoPulse.Mobile.ViewModels;
using ZXing.Net.Maui;

namespace CargoPulse.Mobile.Views;

public partial class ScannerPage : ContentPage
{
    private readonly ScannerViewModel _viewModel;

    public ScannerPage(ScannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var firstCode = e.Results?.FirstOrDefault()?.Value;
        if (!string.IsNullOrWhiteSpace(firstCode))
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _viewModel.ProcessBarcodeDetected(firstCode);
            });
        }
    }
}
