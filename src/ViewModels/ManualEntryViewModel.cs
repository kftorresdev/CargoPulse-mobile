using CargoPulse.Mobile.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoPulse.Mobile.ViewModels;

public partial class ManualEntryViewModel : BaseViewModel
{
    private readonly IShipmentApiClient _shipmentApiClient;

    [ObservableProperty]
    private string _trackingNumber = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    private string _errorMessage = string.Empty;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ManualEntryViewModel(IShipmentApiClient shipmentApiClient)
    {
        _shipmentApiClient = shipmentApiClient ?? throw new ArgumentNullException(nameof(shipmentApiClient));
        Title = "Búsqueda Manual";
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(TrackingNumber) || TrackingNumber.Trim().Length < 3)
        {
            ErrorMessage = "Ingrese al menos 3 caracteres para buscar el tracking.";
            return;
        }

        var cleanTracking = TrackingNumber.Trim();

        try
        {
            IsBusy = true;
            var response = await _shipmentApiClient.FindByTrackingNumberAsync(cleanTracking);
            if (response?.Data != null && !string.IsNullOrWhiteSpace(response.Data.Id))
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync($"shipment/detail?id={response.Data.Id}");
                }
            }
            else
            {
                ErrorMessage = $"No se encontró el embarque con tracking: {cleanTracking}";
            }
        }
        catch (Exception)
        {
            ErrorMessage = $"Error al consultar el embarque: {cleanTracking}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
