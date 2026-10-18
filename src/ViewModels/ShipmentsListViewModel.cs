using System.Collections.ObjectModel;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoPulse.Mobile.ViewModels;

public partial class ShipmentsListViewModel : BaseViewModel
{
    private readonly IShipmentApiClient _shipmentApiClient;

    [ObservableProperty]
    private ObservableCollection<ShipmentDto> _shipments = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    private string _errorMessage = string.Empty;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => !IsBusy && Shipments.Count == 0;

    public ShipmentsListViewModel(IShipmentApiClient shipmentApiClient)
    {
        _shipmentApiClient = shipmentApiClient ?? throw new ArgumentNullException(nameof(shipmentApiClient));
        Title = "Embarques";
    }

    [RelayCommand]
    public async Task LoadShipmentsAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var cleanSearch = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var response = await _shipmentApiClient.GetShipmentsAsync(page: 1, limit: 50, search: cleanSearch);

            Shipments.Clear();
            if (response?.Data?.Items != null)
            {
                foreach (var item in response.Data.Items)
                {
                    Shipments.Add(item);
                }
            }

            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al obtener la lista de embarques: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadShipmentsAsync();
    }

    [RelayCommand]
    public async Task SelectShipmentAsync(ShipmentDto? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Id))
        {
            return;
        }

        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync($"shipment/detail?id={item.Id}");
        }
    }
}
