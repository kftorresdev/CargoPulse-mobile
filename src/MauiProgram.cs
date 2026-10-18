using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Services;
using CargoPulse.Mobile.ViewModels;
using CargoPulse.Mobile.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace CargoPulse.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Core & Data Services
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<IHapticService, HapticService>();
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri(ApiConstants.DefaultBaseUrl),
            Timeout = TimeSpan.FromSeconds(ApiConstants.DefaultTimeoutSeconds)
        });
        builder.Services.AddSingleton<IAuthApiClient, AuthApiClient>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IShipmentApiClient, ShipmentApiClient>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ScannerViewModel>();
        builder.Services.AddTransient<ManualEntryViewModel>();
        builder.Services.AddTransient<ShipmentDetailViewModel>();
        builder.Services.AddTransient<ShipmentsListViewModel>();

        // Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ScannerPage>();
        builder.Services.AddTransient<ManualEntryPage>();
        builder.Services.AddTransient<ShipmentDetailPage>();
        builder.Services.AddTransient<ShipmentsListPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
