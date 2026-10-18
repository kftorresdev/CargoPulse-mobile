using CargoPulse.Mobile.Core.Constants;
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

    [ObservableProperty]
    private string _serverUrl = ApiConstants.BaseUrl;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        Title = "Iniciar Sesión";
    }

    [RelayCommand]
    private async Task ConfigureServerUrlAsync()
    {
        if (Shell.Current != null)
        {
            var result = await Shell.Current.DisplayPromptAsync(
                "Configuración de Servidor",
                "Ingresa la URL base del backend NestJS:\n• Emulador: http://10.0.2.2:3000/api/\n• USB (adb reverse): http://localhost:3000/api/\n• Wi-Fi Local: http://<IP-PC>:3000/api/",
                initialValue: ServerUrl,
                keyboard: Keyboard.Url);

            if (!string.IsNullOrWhiteSpace(result))
            {
                var formatted = result.TrimEnd('/') + "/";
                Preferences.Default.Set(ApiConstants.CustomApiBaseUrlKey, formatted);
                ServerUrl = formatted;
            }
        }
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
                ErrorMessage = !string.IsNullOrWhiteSpace(_authService.LastErrorMessage)
                    ? _authService.LastErrorMessage
                    : "Credenciales incorrectas o error de conexión";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
