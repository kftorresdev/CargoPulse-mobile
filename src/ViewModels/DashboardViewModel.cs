using CargoPulse.Mobile.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoPulse.Mobile.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _operatorName = string.Empty;

    [ObservableProperty]
    private string _operatorRole = string.Empty;

    [ObservableProperty]
    private string _currentDate = string.Empty;

    [ObservableProperty]
    private int _pendingSyncCount;

    public DashboardViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        Title = "Dashboard";
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            CurrentDate = DateTime.Now.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

            var user = await _authService.GetCurrentUserAsync();
            if (user != null)
            {
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                OperatorName = string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
                OperatorRole = string.IsNullOrWhiteSpace(user.Role) ? "LOGISTICS_OPERATOR" : user.Role;
            }
            else
            {
                OperatorName = "Operador";
                OperatorRole = "LOGISTICS_OPERATOR";
            }
        }
        catch (Exception)
        {
            OperatorName = "Operador";
            OperatorRole = "LOGISTICS_OPERATOR";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            IsBusy = true;
            await _authService.LogoutAsync();
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//login");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToScannerAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//main/scanner");
        }
    }

    [RelayCommand]
    private async Task NavigateToSyncQueueAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//main/sync");
        }
    }

    [RelayCommand]
    private async Task NavigateToShipmentsAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//main/shipments");
        }
    }
}
