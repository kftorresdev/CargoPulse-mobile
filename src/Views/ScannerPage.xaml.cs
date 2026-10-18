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

        BarcodeReaderView.Options ??= new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            TryHarder = true
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _viewModel.ResetScanner();

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status == PermissionStatus.Granted)
        {
            if (BarcodeReaderView != null)
            {
                BarcodeReaderView.IsDetecting = true;
            }
            _viewModel.ResetScanner();
        }
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        _viewModel.ResetScanner();
        if (BarcodeReaderView != null)
        {
            BarcodeReaderView.IsDetecting = true;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BarcodeReaderView != null)
        {
            BarcodeReaderView.IsDetecting = false;
        }
        _viewModel.IsScanning = false;
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
