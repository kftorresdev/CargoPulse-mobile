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

        if (BarcodeReaderView != null)
        {
            BarcodeReaderView.IsDetecting = false;
            BarcodeReaderView.Options ??= new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.All,
                AutoRotate = true,
                TryHarder = true
            };
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                _viewModel.ResetScanner();
                if (BarcodeReaderView != null)
                {
                    BarcodeReaderView.IsDetecting = true;
                }
            }
            else
            {
                if (BarcodeReaderView != null)
                {
                    BarcodeReaderView.IsDetecting = false;
                }
                _viewModel.IsScanning = false;
                _viewModel.ErrorMessage = "Permiso de cámara no concedido. Conceda permiso de Cámara en los ajustes del dispositivo o utilice la Búsqueda Manual.";
            }
        }
        catch (Exception ex)
        {
            _viewModel.ErrorMessage = $"Error al inicializar cámara: {ex.Message}";
        }
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
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
