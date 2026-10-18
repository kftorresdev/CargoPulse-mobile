using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Core.Handlers;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Local;
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
        builder.Services.AddSingleton<IMediaService, MediaService>();
        builder.Services.AddSingleton<ISignatureService, SignatureService>();
        builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<AuthenticatedHttpClientHandler>();
        builder.Services.AddSingleton(sp => new HttpClient(sp.GetRequiredService<AuthenticatedHttpClientHandler>())
        {
            BaseAddress = new Uri(ApiConstants.BaseUrl),
            Timeout = TimeSpan.FromSeconds(ApiConstants.DefaultTimeoutSeconds)
        });
        builder.Services.AddSingleton<IAuthApiClient, AuthApiClient>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IShipmentApiClient, ShipmentApiClient>();
        builder.Services.AddSingleton<IInspectionApiClient, InspectionApiClient>();
        builder.Services.AddSingleton<ILocalInspectionRepository, LocalInspectionRepository>();
        builder.Services.AddSingleton<ISyncEngine, BackgroundSyncEngine>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ScannerViewModel>();
        builder.Services.AddTransient<ManualEntryViewModel>();
        builder.Services.AddTransient<ShipmentDetailViewModel>();
        builder.Services.AddTransient<ShipmentsListViewModel>();
        builder.Services.AddTransient<InspectionViewModel>();
        builder.Services.AddTransient<SyncQueueViewModel>();

        // Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ScannerPage>();
        builder.Services.AddTransient<ManualEntryPage>();
        builder.Services.AddTransient<ShipmentDetailPage>();
        builder.Services.AddTransient<ShipmentsListPage>();
        builder.Services.AddTransient<InspectionPage>();
        builder.Services.AddTransient<SyncQueuePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
