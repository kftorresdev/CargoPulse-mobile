using System.Collections.ObjectModel;
using System.Text;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Local;
using CargoPulse.Mobile.Data.Local.Entities;
using CargoPulse.Mobile.Data.Services;
using CargoPulse.Mobile.ViewModels;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class InspectionViewModelTests
{
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<ISignatureService> _mockSignatureService;
    private readonly Mock<IGeolocationService> _mockGeolocationService;
    private readonly Mock<IShipmentApiClient> _mockShipmentApiClient;
    private readonly Mock<ILocalInspectionRepository> _mockLocalRepo;
    private readonly Mock<ISyncEngine> _mockSyncEngine;
    private readonly Mock<IConnectivityService> _mockConnectivityService;
    private readonly InspectionViewModel _sut;

    public InspectionViewModelTests()
    {
        _mockMediaService = new Mock<IMediaService>();
        _mockSignatureService = new Mock<ISignatureService>();
        _mockGeolocationService = new Mock<IGeolocationService>();
        _mockShipmentApiClient = new Mock<IShipmentApiClient>();
        _mockLocalRepo = new Mock<ILocalInspectionRepository>();
        _mockSyncEngine = new Mock<ISyncEngine>();
        _mockConnectivityService = new Mock<IConnectivityService>();

        _mockConnectivityService.Setup(c => c.IsConnected).Returns(true);

        _sut = new InspectionViewModel(
            _mockMediaService.Object,
            _mockSignatureService.Object,
            _mockGeolocationService.Object,
            _mockShipmentApiClient.Object,
            _mockLocalRepo.Object,
            _mockSyncEngine.Object,
            _mockConnectivityService.Object
        );
    }

    [Fact]
    public void InspectionViewModel_InitialState_HasExpectedDefaults()
    {
        _sut.ShipmentId.Should().BeEmpty();
        _sut.TrackingNumber.Should().BeEmpty();
        _sut.CarrierName.Should().BeEmpty();
        _sut.DeclaredSealNumber.Should().BeEmpty();
        _sut.ContainerCondition.Should().Be("GOOD");
        _sut.IsConditionGood.Should().BeTrue();
        _sut.IsConditionRegular.Should().BeFalse();
        _sut.IsConditionDamaged.Should().BeFalse();
        _sut.SealIntact.Should().BeTrue();
        _sut.ActualSealNumber.Should().BeEmpty();
        _sut.SealMismatch.Should().BeFalse();
        _sut.DamagesReported.Should().BeFalse();
        _sut.DamagesDescription.Should().BeNull();
        _sut.Photos.Should().BeEmpty();
        _sut.SignatureBase64.Should().BeEmpty();
        _sut.SignerName.Should().BeEmpty();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.IsSignatureEmpty.Should().BeTrue();
        _sut.LastGeneratedInspection.Should().BeNull();
        _sut.Title.Should().Be("Inspección de Contenedor");
    }

    [Theory]
    [InlineData("GOOD", true, false, false, false)]
    [InlineData("REGULAR", false, true, false, false)]
    [InlineData("DAMAGED", false, false, true, true)]
    public void SetCondition_UpdatesConditionAndDamagesFlags(
        string condition,
        bool expectedGood,
        bool expectedRegular,
        bool expectedDamaged,
        bool expectedDamagesReported)
    {
        _sut.SetCondition(condition);

        _sut.ContainerCondition.Should().Be(condition);
        _sut.IsConditionGood.Should().Be(expectedGood);
        _sut.IsConditionRegular.Should().Be(expectedRegular);
        _sut.IsConditionDamaged.Should().Be(expectedDamaged);
        _sut.DamagesReported.Should().Be(expectedDamagesReported);
    }

    [Fact]
    public void SetCondition_WhenSealNotIntact_FlagsDamagesReportedEvenIfConditionGood()
    {
        _sut.SealIntact = false;
        _sut.SetCondition("GOOD");

        _sut.DamagesReported.Should().BeTrue();
    }

    [Fact]
    public void SealIntactChanged_WhenChangedToFalse_SetsDamagesReportedToTrue()
    {
        _sut.SetCondition("GOOD");
        _sut.DamagesReported.Should().BeFalse();

        _sut.SealIntact = false;
        _sut.DamagesReported.Should().BeTrue();
    }

    [Theory]
    [InlineData("SEAL-001", "", false)]
    [InlineData("SEAL-001", "   ", false)]
    [InlineData("SEAL-001", "SEAL-001", false)]
    [InlineData("SEAL-001", "seal-001", false)]
    [InlineData("SEAL-001", "  SEAL-001  ", false)]
    [InlineData("SEAL-001", "SEAL-999", true)]
    [InlineData("", "SEAL-001", true)]
    public void SealMismatch_EvaluatesCorrectly(string declared, string actual, bool expectedMismatch)
    {
        _sut.DeclaredSealNumber = declared;
        _sut.ActualSealNumber = actual;

        _sut.SealMismatch.Should().Be(expectedMismatch);
    }

    [Fact]
    public async Task CapturePhotoAsync_WhenUnderThreePhotos_AddsPhotoToList()
    {
        const string mockPhotoPath = "C:/photos/photo1.jpg";
        _mockMediaService
            .Setup(m => m.CapturePhotoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPhotoPath);

        await _sut.CapturePhotoAsync();

        _sut.Photos.Should().ContainSingle().Which.Should().Be(mockPhotoPath);
        _mockMediaService.Verify(m => m.CapturePhotoAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CapturePhotoAsync_WhenMaxThreePhotosReached_DoesNotCallMediaService()
    {
        _sut.Photos.Add("p1.jpg");
        _sut.Photos.Add("p2.jpg");
        _sut.Photos.Add("p3.jpg");

        await _sut.CapturePhotoAsync();

        _sut.Photos.Should().HaveCount(3);
        _mockMediaService.Verify(m => m.CapturePhotoAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void RemovePhoto_RemovesItemFromList()
    {
        _sut.Photos.Add("p1.jpg");
        _sut.Photos.Add("p2.jpg");

        _sut.RemovePhoto("p1.jpg");

        _sut.Photos.Should().ContainSingle().Which.Should().Be("p2.jpg");
    }

    [Fact]
    public async Task SaveSignatureAsync_WhenStreamProvided_CallsSignatureServiceAndSetsProperties()
    {
        const string mockBase64 = "data:image/png;base64,AABBCCDDEE==";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("drawing"));

        _mockSignatureService
            .Setup(s => s.ConvertDrawingStreamToBase64Async(It.IsAny<Stream>()))
            .ReturnsAsync(mockBase64);

        await _sut.SaveSignatureAsync(stream);

        _sut.SignatureBase64.Should().Be(mockBase64);
        _sut.IsSignatureEmpty.Should().BeFalse();
    }

    [Fact]
    public void ClearSignature_ResetsSignatureProperties()
    {
        _sut.SignatureBase64 = "data:image/png;base64,AAA==";
        _sut.IsSignatureEmpty = false;

        _sut.ClearSignature();

        _sut.SignatureBase64.Should().BeEmpty();
        _sut.IsSignatureEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitInspectionAsync_WhenActualSealNumberEmpty_SetsExpectedError()
    {
        _sut.ActualSealNumber = string.Empty;
        _sut.SignerName = "Juan Pérez";
        _sut.SignatureBase64 = "data:image/png;base64,AAA==";

        await _sut.SubmitInspectionAsync();

        _sut.ErrorMessage.Should().Be("Debe registrar el número de sello físico verificado");
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.LastGeneratedInspection.Should().BeNull();
    }

    [Fact]
    public async Task SubmitInspectionAsync_WhenSignerNameEmpty_SetsExpectedError()
    {
        _sut.ActualSealNumber = "SEAL-100";
        _sut.SignerName = string.Empty;
        _sut.SignatureBase64 = "data:image/png;base64,AAA==";

        await _sut.SubmitInspectionAsync();

        _sut.ErrorMessage.Should().Be("Debe ingresar el nombre del transportista o receptor");
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.LastGeneratedInspection.Should().BeNull();
    }

    [Fact]
    public async Task SubmitInspectionAsync_WhenSignatureEmpty_SetsExpectedError()
    {
        _sut.ActualSealNumber = "SEAL-100";
        _sut.SignerName = "Juan Pérez";
        _sut.SignatureBase64 = string.Empty;

        await _sut.SubmitInspectionAsync();

        _sut.ErrorMessage.Should().Be("La firma táctil del transportista es obligatoria");
        _sut.HasErrorMessage.Should().BeTrue();
        _sut.LastGeneratedInspection.Should().BeNull();
    }

    [Fact]
    public async Task SubmitInspectionAsync_WhenValid_BuildsInspectionDtoWithGpsAndAssignsLastGenerated()
    {
        // Arrange
        _sut.ShipmentId = "ship-555";
        _sut.DeclaredSealNumber = "SEAL-555-DEC";
        _sut.ActualSealNumber = "SEAL-555-ACT";
        _sut.ContainerCondition = "DAMAGED";
        _sut.DamagesDescription = "Abolladura severa puerta derecha";
        _sut.SignerName = "Carlos Mendoza";
        _sut.SignatureBase64 = "data:image/png;base64,SIGNATURE123";
        _sut.Photos.Add("photo_left.jpg");

        _mockGeolocationService
            .Setup(g => g.GetCurrentCoordinatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("19.4326,-99.1332");

        // Act
        await _sut.SubmitInspectionAsync();

        // Assert
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.HasErrorMessage.Should().BeFalse();
        _sut.LastGeneratedInspection.Should().NotBeNull();

        var inspection = _sut.LastGeneratedInspection!;
        inspection.Id.Should().NotBeNullOrWhiteSpace();
        inspection.ShipmentId.Should().Be("ship-555");
        inspection.ContainerCondition.Should().Be("DAMAGED");
        inspection.SealNumberDeclared.Should().Be("SEAL-555-DEC");
        inspection.SealNumberActual.Should().Be("SEAL-555-ACT");
        inspection.SealMismatch.Should().BeTrue();
        inspection.DamagesReported.Should().BeTrue();
        inspection.DamagesDescription.Should().Be("Abolladura severa puerta derecha");
        inspection.Photos.Should().HaveCount(1);
        inspection.Photos[0].FileName.Should().Be("photo_left.jpg");
        inspection.SignatureBase64.Should().Be("data:image/png;base64,SIGNATURE123");
        inspection.SignerName.Should().Be("Carlos Mendoza");
        inspection.GpsCoordinates.Should().Be("19.4326,-99.1332");
    }

    [Fact]
    public void ApplyQueryAttributes_WhenShipmentIdPassed_LoadsShipmentDetails()
    {
        var shipment = new ShipmentDto(
            Id: "ship-777",
            TrackingNumber: "TRK-777",
            Origin: "Manzanillo",
            Destination: "CDMX",
            CurrentStatus: "IN_TRANSIT",
            CarrierName: "Transportes del Norte",
            Eta: DateTime.UtcNow,
            ActualDelivery: null,
            AlertsCount: 0,
            Stages: null,
            Alerts: null
        );

        _mockShipmentApiClient
            .Setup(api => api.GetShipmentByIdAsync("ship-777", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShipmentDetailResponseDto(true, shipment, DateTime.UtcNow));

        var query = new Dictionary<string, object>
        {
            { "id", "ship-777" }
        };

        _sut.ApplyQueryAttributes(query);

        _sut.ShipmentId.Should().Be("ship-777");
    }

    [Fact]
    public void ApplyQueryAttributes_WhenNoIdPassed_SetsDefaultDeclaredSeal()
    {
        var query = new Dictionary<string, object>();

        _sut.ApplyQueryAttributes(query);

        _sut.DeclaredSealNumber.Should().Be("SEAL-MX-98442");
    }

    [Fact]
    public async Task SubmitInspectionAsync_WhenValidAndOnline_SavesLocalRecordAndTriggersSyncEngine()
    {
        _sut.ShipmentId = "ship-999";
        _sut.TrackingNumber = "TRK-999";
        _sut.DeclaredSealNumber = "SEAL-DEC-999";
        _sut.ActualSealNumber = "SEAL-ACT-999";
        _sut.ContainerCondition = "GOOD";
        _sut.SignerName = "Mario Rossi";
        _sut.SignatureBase64 = "data:image/png;base64,SIGDATA";
        _sut.Photos.Add("photo1.jpg");
        _sut.Photos.Add("photo2.jpg");

        _mockConnectivityService.Setup(c => c.IsConnected).Returns(true);
        _mockLocalRepo.Setup(r => r.SaveInspectionAsync(It.IsAny<LocalInspectionRecord>())).Returns(Task.CompletedTask);
        _mockSyncEngine.Setup(s => s.SyncPendingInspectionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.SubmitInspectionAsync();

        _mockLocalRepo.Verify(r => r.SaveInspectionAsync(It.Is<LocalInspectionRecord>(record =>
            record.ShipmentId == "ship-999" &&
            record.TrackingNumber == "TRK-999" &&
            record.SyncStatus == "PENDING" &&
            record.PhotosJson.Contains("photo1.jpg") &&
            record.PhotosJson.Contains("photo2.jpg") &&
            record.SignerName == "Mario Rossi" &&
            record.SignatureBase64 == "data:image/png;base64,SIGDATA"
        )), Times.Once);

        _mockSyncEngine.Verify(s => s.SyncPendingInspectionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sut.SuccessFeedbackMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SubmitInspectionAsync_WhenOffline_SavesLocalRecordWithoutTriggeringSyncEngine()
    {
        _sut.ShipmentId = "ship-888";
        _sut.TrackingNumber = "TRK-888";
        _sut.DeclaredSealNumber = "SEAL-DEC-888";
        _sut.ActualSealNumber = "SEAL-ACT-888";
        _sut.ContainerCondition = "GOOD";
        _sut.SignerName = "Luigi Verde";
        _sut.SignatureBase64 = "data:image/png;base64,SIGLUIGI";

        _mockConnectivityService.Setup(c => c.IsConnected).Returns(false);
        _mockLocalRepo.Setup(r => r.SaveInspectionAsync(It.IsAny<LocalInspectionRecord>())).Returns(Task.CompletedTask);

        await _sut.SubmitInspectionAsync();

        _mockLocalRepo.Verify(r => r.SaveInspectionAsync(It.Is<LocalInspectionRecord>(record =>
            record.ShipmentId == "ship-888" &&
            record.SyncStatus == "PENDING"
        )), Times.Once);

        _mockSyncEngine.Verify(s => s.SyncPendingInspectionsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _sut.SuccessFeedbackMessage.Should().NotBeNullOrWhiteSpace();
    }
}
