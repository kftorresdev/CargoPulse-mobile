using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoPulse.Mobile.ViewModels;

public partial class ShipmentDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IShipmentApiClient _shipmentApiClient;

    [ObservableProperty]
    private string _shipmentId = string.Empty;

    [ObservableProperty]
    private ShipmentDto? _shipment;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasAlerts;

    [ObservableProperty]
    private int _alertsCount;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ShipmentDetailViewModel(IShipmentApiClient shipmentApiClient)
    {
        _shipmentApiClient = shipmentApiClient ?? throw new ArgumentNullException(nameof(shipmentApiClient));
        Title = "Detalle del Embarque";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) && idObj is string id && !string.IsNullOrWhiteSpace(id))
        {
            ShipmentId = id;
            _ = LoadShipmentAsync(id);
        }
    }

    public async Task LoadShipmentAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            ErrorMessage = "Identificador de embarque inválido.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            ShipmentId = id;

            var response = await _shipmentApiClient.GetShipmentByIdAsync(id);
            if (response?.Data != null)
            {
                Shipment = response.Data;
                AlertsCount = Shipment.Alerts?.Count ?? Shipment.AlertsCount ?? 0;
                HasAlerts = AlertsCount > 0;
            }
            else
            {
                ErrorMessage = "No se encontró el detalle del embarque solicitado.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar el embarque: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (!string.IsNullOrWhiteSpace(ShipmentId))
        {
            await LoadShipmentAsync(ShipmentId);
        }
    }

    [RelayCommand]
    private async Task StartInspectionAsync()
    {
        if (Shipment != null && !string.IsNullOrWhiteSpace(Shipment.Id))
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync($"shipment/inspect?id={Shipment.Id}");
            }
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
