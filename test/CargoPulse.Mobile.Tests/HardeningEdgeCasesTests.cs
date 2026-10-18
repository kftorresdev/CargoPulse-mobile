using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CargoPulse.Mobile.Core.Constants;
using CargoPulse.Mobile.Core.Handlers;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Local;
using CargoPulse.Mobile.Data.Local.Entities;
using CargoPulse.Mobile.Data.Services;
using CargoPulse.Mobile.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace CargoPulse.Mobile.Tests;

/// <summary>
/// Comprehensive QA hardening and edge-case unit test suite covering disposal unsubscriptions,
/// corrupt/missing files, 500 error handling, sync concurrency guards, auth handlers, and input validation.
/// </summary>
public class HardeningEdgeCasesTests : IDisposable
{
    private readonly List<string> _tempFilesToCleanup = new();

    public void Dispose()
    {
        foreach (var file in _tempFilesToCleanup)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }
    }

    #region 1. SyncQueueViewModel Disposal and Event Unsubscription Tests

    [Fact]
    public void SyncQueueViewModel_Dispose_ProperlyUnsubscribesFromSyncEngineAndConnectivityEvents()
    {
        // Arrange
        var mockRepo = new Mock<ILocalInspectionRepository>();
        var mockSyncEngine = new Mock<ISyncEngine>();
        var mockConnectivity = new Mock<IConnectivityService>();

        mockConnectivity.Setup(c => c.IsConnected).Returns(true);
        mockSyncEngine.Setup(s => s.IsSyncing).Returns(false);

        var vm = new SyncQueueViewModel(mockRepo.Object, mockSyncEngine.Object, mockConnectivity.Object);

        // Verify initial state
        vm.IsOnline.Should().BeTrue();
        vm.SyncMessage.Should().BeEmpty();
        vm.ProgressPercent.Should().Be(0.0);

        // Raise events prior to disposal -> VM state should update
        mockSyncEngine.Raise(s => s.SyncProgressChanged += null, new SyncProgressEventArgs
        {
            Message = "Sincronizando 1 de 2",
            Processed = 1,
            Total = 2
        });
        vm.SyncMessage.Should().Be("Sincronizando 1 de 2");
        vm.ProgressPercent.Should().Be(0.5);

        mockConnectivity.Raise(c => c.ConnectivityChanged += null, mockConnectivity.Object, false);
        vm.IsOnline.Should().BeFalse();

        // Act - Dispose the ViewModel
        vm.Dispose();

        // Raise events after disposal -> VM state MUST remain unchanged
        mockSyncEngine.Raise(s => s.SyncProgressChanged += null, new SyncProgressEventArgs
        {
            Message = "Mensaje posterior al dispose",
            Processed = 2,
            Total = 2
        });
        mockConnectivity.Raise(c => c.ConnectivityChanged += null, mockConnectivity.Object, true);

        // Assert: Properties still hold the values prior to Dispose
        vm.SyncMessage.Should().Be("Sincronizando 1 de 2");
        vm.ProgressPercent.Should().Be(0.5);
        vm.IsOnline.Should().BeFalse();
    }

    [Fact]
    public void SyncQueueViewModel_Dispose_CalledMultipleTimes_IsIdempotentAndDoesNotThrow()
    {
        // Arrange
        var mockRepo = new Mock<ILocalInspectionRepository>();
        var mockSyncEngine = new Mock<ISyncEngine>();
        var mockConnectivity = new Mock<IConnectivityService>();

        var vm = new SyncQueueViewModel(mockRepo.Object, mockSyncEngine.Object, mockConnectivity.Object);

        // Act & Assert
        Action act = () =>
        {
            vm.Dispose();
            vm.Dispose();
            vm.Dispose();
        };

        act.Should().NotThrow();
    }

    #endregion

    #region 2. BackgroundSyncEngine Missing or Corrupt Photo Handling Tests

    [Fact]
    public async Task BackgroundSyncEngine_WhenPhotoFileDeleted_DoesNotCrashAndHandlesGracefully()
    {
        // Arrange
        var mockRepo = new Mock<ILocalInspectionRepository>();
        var mockApiClient = new Mock<IInspectionApiClient>();
        var mockConnectivity = new Mock<IConnectivityService>();
        mockConnectivity.Setup(c => c.IsConnected).Returns(true);

        // Create a non-existent file path simulating deleted image
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"deleted_photo_{Guid.NewGuid():N}.jpg");
        if (File.Exists(nonExistentPath))
        {
            File.Delete(nonExistentPath);
        }

        var photosJson = JsonSerializer.Serialize(new[] { nonExistentPath });
        var record = new LocalInspectionRecord
        {
            Id = "insp-deleted-photo",
            ShipmentId = "ship-001",
            PhotosJson = photosJson,
            ContainerCondition = "GOOD",
            SealIntact = true,
            DeclaredSealNumber = "SEAL-1",
            ActualSealNumber = "SEAL-1",
            SignerName = "Operator Test",
            SignatureBase64 = "sig123"
        };

        mockRepo.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { record });

        CreateInspectionRequestDto? capturedDto = null;
        var syncResponse = new InspectionSyncResponseDto(
            true,
            new InspectionSyncResultDto("insp-deleted-photo", "ship-001", "SYNCED", DateTime.UtcNow, false),
            DateTime.UtcNow);

        mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateInspectionRequestDto, CancellationToken>((dto, _) => capturedDto = dto)
            .ReturnsAsync(syncResponse);

        using var engine = new BackgroundSyncEngine(mockRepo.Object, mockApiClient.Object, mockConnectivity.Object);

        // Act - Executing sync should not crash or throw unhandled exceptions
        var result = await engine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(1);
        capturedDto.Should().NotBeNull();
        capturedDto!.Photos.Should().HaveCount(1);
        capturedDto.Photos[0].FileName.Should().NotBeNullOrWhiteSpace();
        mockRepo.Verify(r => r.MarkAsSyncedAsync("insp-deleted-photo", It.IsAny<DateTime>()), Times.Once);
        mockRepo.Verify(r => r.MarkAsFailedAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task BackgroundSyncEngine_WhenPhotosJsonIsCorrupt_DoesNotCrashAndHandlesGracefully()
    {
        // Arrange
        var mockRepo = new Mock<ILocalInspectionRepository>();
        var mockApiClient = new Mock<IInspectionApiClient>();
        var mockConnectivity = new Mock<IConnectivityService>();
        mockConnectivity.Setup(c => c.IsConnected).Returns(true);

        var corruptPhotosJson = "{ corrupt_json: [ invalid-structure }";
        var record = new LocalInspectionRecord
        {
            Id = "insp-corrupt-json",
            ShipmentId = "ship-002",
            PhotosJson = corruptPhotosJson,
            ContainerCondition = "GOOD",
            SealIntact = true,
            DeclaredSealNumber = "SEAL-2",
            ActualSealNumber = "SEAL-2",
            SignerName = "Operator Test",
            SignatureBase64 = "sig123"
        };

        mockRepo.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { record });

        CreateInspectionRequestDto? capturedDto = null;
        var syncResponse = new InspectionSyncResponseDto(
            true,
            new InspectionSyncResultDto("insp-corrupt-json", "ship-002", "SYNCED", DateTime.UtcNow, false),
            DateTime.UtcNow);

        mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateInspectionRequestDto, CancellationToken>((dto, _) => capturedDto = dto)
            .ReturnsAsync(syncResponse);

        using var engine = new BackgroundSyncEngine(mockRepo.Object, mockApiClient.Object, mockConnectivity.Object);

        // Act
        var result = await engine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(1);
        capturedDto.Should().NotBeNull();
        capturedDto!.Photos.Should().BeEmpty(); // Corrupt JSON gracefully falls back to empty photo list
        mockRepo.Verify(r => r.MarkAsSyncedAsync("insp-corrupt-json", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task BackgroundSyncEngine_WhenPhotoProcessingFailsWithException_DoesNotCrashAndMarksAsFailed()
    {
        // Arrange
        var mockRepo = new Mock<ILocalInspectionRepository>();
        var mockApiClient = new Mock<IInspectionApiClient>();
        var mockConnectivity = new Mock<IConnectivityService>();
        mockConnectivity.Setup(c => c.IsConnected).Returns(true);

        var record = new LocalInspectionRecord
        {
            Id = "insp-photo-error",
            ShipmentId = "ship-003",
            PhotosJson = "[]"
        };

        mockRepo.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { record });

        mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk read error: The photo file is corrupt or locked."));

        using var engine = new BackgroundSyncEngine(mockRepo.Object, mockApiClient.Object, mockConnectivity.Object);

        // Act
        var result = await engine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        engine.IsSyncing.Should().BeFalse();
        mockRepo.Verify(r => r.MarkAsFailedAsync("insp-photo-error", It.Is<string>(msg => msg.Contains("Disk read error"))), Times.Once);
    }

    #endregion

    #region 3. BackgroundSyncEngine 500 Internal Server Error Handling Tests

    [Fact]
    public async Task BackgroundSyncEngine_WhenApiReturns500InternalServerError_MarksRecordAsFailedAndCapturesErrorMessage()
    {
        // Arrange
        var mockRepo = new Mock<ILocalInspectionRepository>();
        var mockApiClient = new Mock<IInspectionApiClient>();
        var mockConnectivity = new Mock<IConnectivityService>();
        mockConnectivity.Setup(c => c.IsConnected).Returns(true);

        var record = new LocalInspectionRecord
        {
            Id = "insp-500",
            ShipmentId = "ship-500",
            PhotosJson = "[]"
        };

        mockRepo.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { record });

        mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Response status code does not indicate success: 500 (Internal Server Error).", null, HttpStatusCode.InternalServerError));

        using var engine = new BackgroundSyncEngine(mockRepo.Object, mockApiClient.Object, mockConnectivity.Object);
        var progressEvents = new List<SyncProgressEventArgs>();
        engine.SyncProgressChanged += (_, args) => progressEvents.Add(args);

        // Act
        var result = await engine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        engine.IsSyncing.Should().BeFalse();

        mockRepo.Verify(r => r.MarkAsFailedAsync("insp-500", It.Is<string>(msg => msg.Contains("500") && msg.Contains("Internal Server Error"))), Times.Once);
        mockRepo.Verify(r => r.MarkAsSyncedAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);

        progressEvents.Should().HaveCount(1);
        progressEvents[0].Failed.Should().Be(1);
        progressEvents[0].Succeeded.Should().Be(0);
        progressEvents[0].Message.Should().Contain("insp-500").And.Contain("500");
    }

    [Fact]
    public async Task BackgroundSyncEngine_WhenApiResponseIndicatesFailureOrNullData_MarksRecordAsFailed()
    {
        // Arrange
        var mockRepo = new Mock<ILocalInspectionRepository>();
        var mockApiClient = new Mock<IInspectionApiClient>();
        var mockConnectivity = new Mock<IConnectivityService>();
        mockConnectivity.Setup(c => c.IsConnected).Returns(true);

        var record = new LocalInspectionRecord
        {
            Id = "insp-failure-response",
            ShipmentId = "ship-501",
            PhotosJson = "[]"
        };

        mockRepo.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { record });

        // API returns response with Success = false and null data
        var failedResponse = new InspectionSyncResponseDto(false, null!, DateTime.UtcNow);
        mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        using var engine = new BackgroundSyncEngine(mockRepo.Object, mockApiClient.Object, mockConnectivity.Object);

        // Act
        var result = await engine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        mockRepo.Verify(r => r.MarkAsFailedAsync("insp-failure-response", It.Is<string>(msg => msg.Contains("failure") || msg.Contains("empty"))), Times.Once);
        mockRepo.Verify(r => r.MarkAsSyncedAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    #endregion

    #region 4. BackgroundSyncEngine Concurrency Tests

    [Fact]
    public async Task BackgroundSyncEngine_Concurrency_SecondCallWhileFirstIsRunning_ExitsImmediatelyReturningZero()
    {
        // Arrange
        var mockRepo = new Mock<ILocalInspectionRepository>();
        var mockApiClient = new Mock<IInspectionApiClient>();
        var mockConnectivity = new Mock<IConnectivityService>();
        mockConnectivity.Setup(c => c.IsConnected).Returns(true);

        var record = new LocalInspectionRecord
        {
            Id = "insp-concurrent",
            ShipmentId = "ship-999",
            PhotosJson = "[]"
        };

        mockRepo.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { record });

        // Use TaskCompletionSource to hold the first call inside the critical section
        var uploadTcs = new TaskCompletionSource<InspectionSyncResponseDto>();
        mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .Returns(uploadTcs.Task);

        using var engine = new BackgroundSyncEngine(mockRepo.Object, mockApiClient.Object, mockConnectivity.Object);

        // Act
        // 1) Start first sync call (which will await uploadTcs)
        var firstCallTask = engine.SyncPendingInspectionsAsync();

        // Verify first call is actively syncing
        engine.IsSyncing.Should().BeTrue();

        // 2) Trigger concurrent second call while first call is in flight
        var secondCallTask = engine.SyncPendingInspectionsAsync();
        var secondCallResult = await secondCallTask;

        // Assert: Second call must immediately return 0 without executing duplicate sync
        secondCallResult.Should().Be(0);

        // 3) Complete first call successfully
        var syncResponse = new InspectionSyncResponseDto(
            true,
            new InspectionSyncResultDto("insp-concurrent", "ship-999", "SYNCED", DateTime.UtcNow, false),
            DateTime.UtcNow);
        uploadTcs.SetResult(syncResponse);

        var firstCallResult = await firstCallTask;

        // Assert: First call finishes successfully and engine releases lock
        firstCallResult.Should().Be(1);
        engine.IsSyncing.Should().BeFalse();

        // Only one upload was performed across both calls
        mockApiClient.Verify(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region 5. AuthenticatedHttpClientHandler Token Header Tests

    private class FakeInnerHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeInnerHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task AuthenticatedHttpClientHandler_WhenTokenIsPresent_AddsBearerAuthorizationHeader()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey))
            .ReturnsAsync("valid-jwt-token-xyz");

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new FakeInnerHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler = new AuthenticatedHttpClientHandler(mockStorage.Object, innerHandler);
        var invoker = new HttpMessageInvoker(handler);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.cargopulse.com/v1/shipments");
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization!.Parameter.Should().Be("valid-jwt-token-xyz");
    }

    [Fact]
    public async Task AuthenticatedHttpClientHandler_WhenTokenIsNull_DoesNotAddAuthorizationHeader()
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey))
            .ReturnsAsync((string?)null);

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new FakeInnerHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler = new AuthenticatedHttpClientHandler(mockStorage.Object, innerHandler);
        var invoker = new HttpMessageInvoker(handler);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.cargopulse.com/v1/shipments");
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task AuthenticatedHttpClientHandler_WhenTokenIsEmptyOrWhitespace_DoesNotAddAuthorizationHeader(string emptyOrWhitespaceToken)
    {
        // Arrange
        var mockStorage = new Mock<ISecureStorageService>();
        mockStorage.Setup(s => s.GetAsync(ApiConstants.AuthTokenKey))
            .ReturnsAsync(emptyOrWhitespaceToken);

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new FakeInnerHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler = new AuthenticatedHttpClientHandler(mockStorage.Object, innerHandler);
        var invoker = new HttpMessageInvoker(handler);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.cargopulse.com/v1/shipments");
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().BeNull();
    }

    #endregion

    #region 6. InspectionViewModel Validation and Empty/Whitespace Reset Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task InspectionViewModel_SubmitInspection_WhenActualSealNumberIsEmptyOrWhitespace_SetsErrorMessageAndDoesNotSave(string? invalidActualSeal)
    {
        // Arrange
        var mockMedia = new Mock<IMediaService>();
        var mockSig = new Mock<ISignatureService>();
        var mockGeo = new Mock<IGeolocationService>();
        var mockShipment = new Mock<IShipmentApiClient>();
        var mockRepo = new Mock<ILocalInspectionRepository>();

        var vm = new InspectionViewModel(mockMedia.Object, mockSig.Object, mockGeo.Object, mockShipment.Object, mockRepo.Object);
        vm.ActualSealNumber = invalidActualSeal!;
        vm.SignerName = "Carlos Gomez";
        vm.SignatureBase64 = "data:image/png;base64,validSignature";

        // Act
        await vm.SubmitInspectionAsync();

        // Assert
        vm.ErrorMessage.Should().Be("Debe registrar el número de sello físico verificado");
        vm.HasErrorMessage.Should().BeTrue();
        vm.LastGeneratedInspection.Should().BeNull();
        mockRepo.Verify(r => r.SaveInspectionAsync(It.IsAny<LocalInspectionRecord>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task InspectionViewModel_SubmitInspection_WhenSignerNameIsEmptyOrWhitespace_SetsErrorMessageAndDoesNotSave(string? invalidSignerName)
    {
        // Arrange
        var mockMedia = new Mock<IMediaService>();
        var mockSig = new Mock<ISignatureService>();
        var mockGeo = new Mock<IGeolocationService>();
        var mockShipment = new Mock<IShipmentApiClient>();
        var mockRepo = new Mock<ILocalInspectionRepository>();

        var vm = new InspectionViewModel(mockMedia.Object, mockSig.Object, mockGeo.Object, mockShipment.Object, mockRepo.Object);
        vm.ActualSealNumber = "SEAL-12345";
        vm.SignerName = invalidSignerName!;
        vm.SignatureBase64 = "data:image/png;base64,validSignature";

        // Act
        await vm.SubmitInspectionAsync();

        // Assert
        vm.ErrorMessage.Should().Be("Debe ingresar el nombre del transportista o receptor");
        vm.HasErrorMessage.Should().BeTrue();
        vm.LastGeneratedInspection.Should().BeNull();
        mockRepo.Verify(r => r.SaveInspectionAsync(It.IsAny<LocalInspectionRecord>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task InspectionViewModel_SubmitInspection_WhenSignatureIsEmptyOrWhitespace_SetsErrorMessageAndDoesNotSave(string? invalidSignature)
    {
        // Arrange
        var mockMedia = new Mock<IMediaService>();
        var mockSig = new Mock<ISignatureService>();
        var mockGeo = new Mock<IGeolocationService>();
        var mockShipment = new Mock<IShipmentApiClient>();
        var mockRepo = new Mock<ILocalInspectionRepository>();

        var vm = new InspectionViewModel(mockMedia.Object, mockSig.Object, mockGeo.Object, mockShipment.Object, mockRepo.Object);
        vm.ActualSealNumber = "SEAL-12345";
        vm.SignerName = "Carlos Gomez";
        vm.SignatureBase64 = invalidSignature!;

        // Act
        await vm.SubmitInspectionAsync();

        // Assert
        vm.ErrorMessage.Should().Be("La firma táctil del transportista es obligatoria");
        vm.HasErrorMessage.Should().BeTrue();
        vm.LastGeneratedInspection.Should().BeNull();
        mockRepo.Verify(r => r.SaveInspectionAsync(It.IsAny<LocalInspectionRecord>()), Times.Never);
    }

    [Fact]
    public void InspectionViewModel_ClearSignature_ResetsSignatureBase64AndSetsIsSignatureEmptyTrue()
    {
        // Arrange
        var mockMedia = new Mock<IMediaService>();
        var mockSig = new Mock<ISignatureService>();
        var mockGeo = new Mock<IGeolocationService>();
        var mockShipment = new Mock<IShipmentApiClient>();

        var vm = new InspectionViewModel(mockMedia.Object, mockSig.Object, mockGeo.Object, mockShipment.Object);
        vm.SignatureBase64 = "data:image/png;base64,existing-sig-123";
        vm.IsSignatureEmpty = false;

        // Act
        vm.ClearSignatureCommand.Execute(null);

        // Assert
        vm.SignatureBase64.Should().BeEmpty();
        vm.IsSignatureEmpty.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InspectionViewModel_SetCondition_WhenNullOrWhitespace_LeavesContainerConditionUnchanged(string? invalidCondition)
    {
        // Arrange
        var mockMedia = new Mock<IMediaService>();
        var mockSig = new Mock<ISignatureService>();
        var mockGeo = new Mock<IGeolocationService>();
        var mockShipment = new Mock<IShipmentApiClient>();

        var vm = new InspectionViewModel(mockMedia.Object, mockSig.Object, mockGeo.Object, mockShipment.Object);
        vm.SetCondition("DAMAGED");
        vm.ContainerCondition.Should().Be("DAMAGED");

        // Act
        vm.SetCondition(invalidCondition!);

        // Assert: ContainerCondition must remain unmodified
        vm.ContainerCondition.Should().Be("DAMAGED");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InspectionViewModel_LoadShipmentAsync_WhenNullOrWhitespace_ExitsImmediatelyWithoutCallingApi(string? invalidId)
    {
        // Arrange
        var mockMedia = new Mock<IMediaService>();
        var mockSig = new Mock<ISignatureService>();
        var mockGeo = new Mock<IGeolocationService>();
        var mockShipment = new Mock<IShipmentApiClient>();

        var vm = new InspectionViewModel(mockMedia.Object, mockSig.Object, mockGeo.Object, mockShipment.Object);

        // Act
        await vm.LoadShipmentAsync(invalidId!);

        // Assert
        mockShipment.Verify(api => api.GetShipmentByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void InspectionViewModel_SealMismatch_EvaluatesWhitespaceCorrectly()
    {
        // Arrange
        var mockMedia = new Mock<IMediaService>();
        var mockSig = new Mock<ISignatureService>();
        var mockGeo = new Mock<IGeolocationService>();
        var mockShipment = new Mock<IShipmentApiClient>();

        var vm = new InspectionViewModel(mockMedia.Object, mockSig.Object, mockGeo.Object, mockShipment.Object);
        vm.DeclaredSealNumber = "SEAL-MX-123";

        // When actual seal is empty or whitespace -> SealMismatch is false
        vm.ActualSealNumber = string.Empty;
        vm.SealMismatch.Should().BeFalse();

        vm.ActualSealNumber = "   ";
        vm.SealMismatch.Should().BeFalse();

        // When actual seal matches after trimming whitespace -> SealMismatch is false
        vm.ActualSealNumber = "  SEAL-MX-123  ";
        vm.SealMismatch.Should().BeFalse();

        // When actual seal is different -> SealMismatch is true
        vm.ActualSealNumber = "SEAL-MX-999";
        vm.SealMismatch.Should().BeTrue();
    }

    [Fact]
    public void InspectionViewModel_RemovePhoto_WhenNullOrWhitespaceOrMissing_HandlesGracefullyWithoutThrowing()
    {
        // Arrange
        var mockMedia = new Mock<IMediaService>();
        var mockSig = new Mock<ISignatureService>();
        var mockGeo = new Mock<IGeolocationService>();
        var mockShipment = new Mock<IShipmentApiClient>();

        var vm = new InspectionViewModel(mockMedia.Object, mockSig.Object, mockGeo.Object, mockShipment.Object);
        vm.Photos.Add("photo_real.jpg");

        // Act & Assert
        Action act = () =>
        {
            vm.RemovePhoto(null);
            vm.RemovePhoto(string.Empty);
            vm.RemovePhoto("   ");
            vm.RemovePhoto("non_existent_photo.jpg");
        };

        act.Should().NotThrow();
        vm.Photos.Should().ContainSingle().Which.Should().Be("photo_real.jpg");

        vm.RemovePhoto("photo_real.jpg");
        vm.Photos.Should().BeEmpty();
    }

    #endregion
}
