using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoPulse.Mobile.ViewModels;

public partial class ScannerViewModel : BaseViewModel
{
    private readonly IShipmentApiClient _shipmentApiClient;
    private readonly IHapticService _hapticService;

    [ObservableProperty]
    private bool _isScanning = true;

    [ObservableProperty]
    private bool _isTorchOn;

    [ObservableProperty]
    private string _detectedBarcode = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    private string _errorMessage = string.Empty;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ScannerViewModel(IShipmentApiClient shipmentApiClient, IHapticService hapticService)
    {
        _shipmentApiClient = shipmentApiClient ?? throw new ArgumentNullException(nameof(shipmentApiClient));
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        Title = "Escáner Óptico";
    }

    public async Task ProcessBarcodeDetected(string barcode)
    {
        await ProcessBarcodeAsync(barcode);
    }

    [RelayCommand]
    public async Task ProcessBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return;
        }

        if (!IsScanning && IsBusy)
        {
            return;
        }

        var cleanBarcode = barcode.Trim();
        DetectedBarcode = cleanBarcode;
        IsScanning = false;
        _hapticService.Success();
        ErrorMessage = string.Empty;

        try
        {
            IsBusy = true;
            var response = await _shipmentApiClient.FindByTrackingNumberAsync(cleanBarcode);
            if (response?.Data != null && !string.IsNullOrWhiteSpace(response.Data.Id))
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync($"shipment/detail?id={response.Data.Id}");
                }
            }
            else
            {
                ErrorMessage = $"No se encontró el embarque con tracking: {cleanBarcode}";
            }
        }
        catch (Exception)
        {
            ErrorMessage = $"No se encontró el embarque con tracking: {cleanBarcode}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleTorch()
    {
        IsTorchOn = !IsTorchOn;
    }

    [RelayCommand]
    public void ResetScanner()
    {
        ErrorMessage = string.Empty;
        DetectedBarcode = string.Empty;
        IsBusy = false;
        IsScanning = true;
    }

    [RelayCommand]
    private void ResumeScanning()
    {
        ResetScanner();
    }

    [RelayCommand]
    private async Task NavigateToManualEntryAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("scanner/manual");
        }
    }
}
