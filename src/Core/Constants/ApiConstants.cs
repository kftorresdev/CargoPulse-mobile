namespace CargoPulse.Mobile.Core.Constants;

public static class ApiConstants
{
    // Default fallback base URL (overridden by settings or platform)
#if ANDROID
    public const string DefaultBaseUrl = "http://10.0.2.2:3000/api";
#else
    public const string DefaultBaseUrl = "http://localhost:3000/api";
#endif

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
