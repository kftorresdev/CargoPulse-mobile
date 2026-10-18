namespace CargoPulse.Mobile.Core.Constants;

public static class ApiConstants
{
    public static string DefaultBaseUrl
    {
        get
        {
#if ANDROID
            try
            {
                if (Microsoft.Maui.Devices.DeviceInfo.Current.DeviceType == Microsoft.Maui.Devices.DeviceType.Physical)
                {
                    return "http://localhost:3000/api/";
                }
            }
            catch
            {
                // Fallback for headless test environments where DeviceInfo is not initialized
            }

            return "http://10.0.2.2:3000/api/";
#else
            return "http://localhost:3000/api/";
#endif
        }
    }

    public const string CustomApiBaseUrlKey = "cargopulse_custom_api_base_url";

    public static string BaseUrl
    {
        get
        {
            try
            {
                var custom = Microsoft.Maui.Storage.Preferences.Default.Get(CustomApiBaseUrlKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(custom))
                {
                    return custom.TrimEnd('/') + "/";
                }
            }
            catch
            {
                // Fallback for headless environments without Preferences provider
            }

            return DefaultBaseUrl;
        }
    }

    public const int DefaultTimeoutSeconds = 15;
    public const int MaxOfflineRetries = 3;

    // SecureStorage Keys
    public const string AuthTokenKey = "cargopulse_auth_token";
    public const string UserRoleKey = "cargopulse_user_role";
    public const string UserIdKey = "cargopulse_user_id";
    public const string UserEmailKey = "cargopulse_user_email";
    public const string UserFirstNameKey = "cargopulse_user_first_name";
    public const string UserLastNameKey = "cargopulse_user_last_name";
    public const string UserNameKey = "cargopulse_user_name";

    // Endpoints
    public const string LoginEndpoint = "auth/login";
    public const string ShipmentsEndpoint = "shipments";
    public const string AlertsEndpoint = "alerts";
    public const string InspectionsEndpoint = "inspections";
    public const string NotesEndpoint = "collaboration/shipment";
}
