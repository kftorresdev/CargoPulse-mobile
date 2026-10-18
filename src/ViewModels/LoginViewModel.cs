using CargoPulse.Mobile.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoPulse.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isPasswordVisible;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        Title = "Iniciar Sesión";
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "El correo y la contraseña son requeridos";
            return;
        }

        try
        {
            IsBusy = true;
            var success = await _authService.LoginAsync(Email.Trim(), Password);
            if (success)
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("//main/dashboard");
                }
            }
            else
            {
                ErrorMessage = "Credenciales incorrectas o error de conexión";
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Credenciales incorrectas o error de conexión";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
