using System.Collections.ObjectModel;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CargoPulse.Mobile.ViewModels;

public partial class InspectionViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IMediaService _mediaService;
    private readonly ISignatureService _signatureService;
    private readonly IGeolocationService _geolocationService;
    private readonly IShipmentApiClient _shipmentApiClient;

    [ObservableProperty]
    private string _shipmentId = string.Empty;

    [ObservableProperty]
    private string _trackingNumber = string.Empty;

    [ObservableProperty]
    private string _carrierName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SealMismatch))]
    private string _declaredSealNumber = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConditionGood))]
    [NotifyPropertyChangedFor(nameof(IsConditionRegular))]
    [NotifyPropertyChangedFor(nameof(IsConditionDamaged))]
    private string _containerCondition = "GOOD";

    [ObservableProperty]
    private bool _sealIntact = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SealMismatch))]
    private string _actualSealNumber = string.Empty;

    [ObservableProperty]
    private bool _damagesReported;

    [ObservableProperty]
    private string? _damagesDescription;

    [ObservableProperty]
    private ObservableCollection<string> _photos = new();

    [ObservableProperty]
    private string _signatureBase64 = string.Empty;

    [ObservableProperty]
    private string _signerName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isSignatureEmpty = true;

    [ObservableProperty]
    private CreateInspectionRequestDto? _lastGeneratedInspection;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsConditionGood => string.Equals(ContainerCondition, "GOOD", StringComparison.OrdinalIgnoreCase);
    public bool IsConditionRegular => string.Equals(ContainerCondition, "REGULAR", StringComparison.OrdinalIgnoreCase);
    public bool IsConditionDamaged => string.Equals(ContainerCondition, "DAMAGED", StringComparison.OrdinalIgnoreCase);

    public bool SealMismatch =>
        !string.IsNullOrWhiteSpace(ActualSealNumber) &&
        !string.Equals(ActualSealNumber.Trim(), DeclaredSealNumber?.Trim(), StringComparison.OrdinalIgnoreCase);

    public InspectionViewModel(
        IMediaService mediaService,
        ISignatureService signatureService,
        IGeolocationService geolocationService,
        IShipmentApiClient shipmentApiClient)
    {
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
        _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
        _shipmentApiClient = shipmentApiClient ?? throw new ArgumentNullException(nameof(shipmentApiClient));
        Title = "Inspección de Contenedor";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) && idObj is string id && !string.IsNullOrWhiteSpace(id))
        {
            ShipmentId = id;
            _ = LoadShipmentAsync(id);
        }
        else if (string.IsNullOrWhiteSpace(DeclaredSealNumber))
        {
            DeclaredSealNumber = "SEAL-MX-98442";
        }

        if (query.TryGetValue("declaredSealNumber", out var sealObj) && sealObj is string seal && !string.IsNullOrWhiteSpace(seal))
        {
            DeclaredSealNumber = seal;
        }

        if (query.TryGetValue("trackingNumber", out var trkObj) && trkObj is string trk && !string.IsNullOrWhiteSpace(trk))
        {
            TrackingNumber = trk;
        }

        if (query.TryGetValue("carrierName", out var carrierObj) && carrierObj is string carrier && !string.IsNullOrWhiteSpace(carrier))
        {
            CarrierName = carrier;
        }
    }

    public async Task LoadShipmentAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
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
                TrackingNumber = response.Data.TrackingNumber ?? string.Empty;
                CarrierName = response.Data.CarrierName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(DeclaredSealNumber))
                {
                    DeclaredSealNumber = !string.IsNullOrWhiteSpace(TrackingNumber) ? $"SEAL-{TrackingNumber}" : "SEAL-MX-98442";
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(DeclaredSealNumber))
                {
                    DeclaredSealNumber = "SEAL-MX-98442";
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar información del embarque: {ex.Message}";
            if (string.IsNullOrWhiteSpace(DeclaredSealNumber))
            {
                DeclaredSealNumber = "SEAL-MX-98442";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnContainerConditionChanged(string value)
    {
        DamagesReported = string.Equals(value, "DAMAGED", StringComparison.OrdinalIgnoreCase) || !SealIntact;
    }

    partial void OnSealIntactChanged(bool value)
    {
        DamagesReported = string.Equals(ContainerCondition, "DAMAGED", StringComparison.OrdinalIgnoreCase) || !value;
    }

    [RelayCommand]
    public void SetCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return;
        }

        var normalized = condition.Trim().ToUpperInvariant();
        ContainerCondition = normalized;
        DamagesReported = (normalized == "DAMAGED" || !SealIntact);
    }

    [RelayCommand]
    public async Task CapturePhotoAsync()
    {
        if (Photos.Count >= 3)
        {
            return;
        }

        try
        {
            var photoPath = await _mediaService.CapturePhotoAsync();
            if (!string.IsNullOrWhiteSpace(photoPath))
            {
                Photos.Add(photoPath);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al capturar fotografía: {ex.Message}";
        }
    }

    [RelayCommand]
    public void RemovePhoto(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Photos.Contains(path))
        {
            Photos.Remove(path);
        }
    }

    [RelayCommand]
    public async Task SaveSignatureAsync(Stream? stream)
    {
        if (stream == null)
        {
            return;
        }

        try
        {
            var base64 = await _signatureService.ConvertDrawingStreamToBase64Async(stream);
            if (!string.IsNullOrWhiteSpace(base64))
            {
                SignatureBase64 = base64;
                IsSignatureEmpty = false;
                ErrorMessage = string.Empty;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al procesar la firma: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ClearSignature()
    {
        SignatureBase64 = string.Empty;
        IsSignatureEmpty = true;
    }

    [RelayCommand]
    public async Task SubmitInspectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ActualSealNumber))
        {
            ErrorMessage = "Debe registrar el número de sello físico verificado";
            return;
        }

        if (string.IsNullOrWhiteSpace(SignerName))
        {
            ErrorMessage = "Debe ingresar el nombre del transportista o receptor";
            return;
        }

        if (string.IsNullOrWhiteSpace(SignatureBase64))
        {
            ErrorMessage = "La firma táctil del transportista es obligatoria";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            string? gpsCoordinates = null;
            try
            {
                gpsCoordinates = await _geolocationService.GetCurrentCoordinatesAsync();
            }
            catch
            {
                // Fallback location
            }

            var photoUploads = Photos.Select((path, index) => new PhotoUploadDto(
                FileName: Path.GetFileName(path) is { Length: > 0 } fn ? fn : $"photo_{index + 1}.jpg",
                ContentType: "image/jpeg",
                Base64Content: path
            )).ToList();

            var inspection = new CreateInspectionRequestDto(
                Id: Guid.NewGuid().ToString(),
                ShipmentId: ShipmentId,
                ContainerCondition: ContainerCondition,
                SealIntact: SealIntact,
                SealNumberDeclared: DeclaredSealNumber,
                SealNumberActual: ActualSealNumber,
                SealMismatch: SealMismatch,
                DamagesReported: DamagesReported,
                DamagesDescription: DamagesDescription,
                Photos: photoUploads,
                SignatureBase64: SignatureBase64,
                SignerName: SignerName,
                GpsCoordinates: gpsCoordinates,
                InspectedAt: DateTime.UtcNow
            );

            LastGeneratedInspection = inspection;

            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//main/dashboard");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al registrar inspección: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
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
