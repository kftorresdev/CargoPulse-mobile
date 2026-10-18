using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Local;
using CargoPulse.Mobile.Data.Local.Entities;
using CargoPulse.Mobile.Data.Services;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class SyncEngineTests : IDisposable
{
    private readonly Mock<ILocalInspectionRepository> _mockRepository;
    private readonly Mock<IInspectionApiClient> _mockApiClient;
    private readonly Mock<IConnectivityService> _mockConnectivityService;
    private readonly BackgroundSyncEngine _syncEngine;

    public SyncEngineTests()
    {
        _mockRepository = new Mock<ILocalInspectionRepository>();
        _mockApiClient = new Mock<IInspectionApiClient>();
        _mockConnectivityService = new Mock<IConnectivityService>();

        _mockConnectivityService.Setup(c => c.IsConnected).Returns(true);

        _syncEngine = new BackgroundSyncEngine(
            _mockRepository.Object,
            _mockApiClient.Object,
            _mockConnectivityService.Object);
    }

    public void Dispose()
    {
        _syncEngine.Dispose();
    }

    private static LocalInspectionRecord CreateSampleRecord(string id = "insp-001", string shipmentId = "ship-100")
    {
        return new LocalInspectionRecord
        {
            Id = id,
            ShipmentId = shipmentId,
            TrackingNumber = "TRK-001",
            ContainerCondition = "GOOD",
            SealIntact = true,
            DeclaredSealNumber = "SEAL-A",
            ActualSealNumber = "SEAL-A",
            SealMismatch = false,
            DamagesReported = false,
            DamagesDescription = null,
            PhotosJson = "[]",
            SignatureBase64 = "data:image/png;base64,abc",
            SignerName = "Juan Perez",
            GpsCoordinates = "19.4326,-99.1332",
            SyncStatus = "PENDING",
            RetryCount = 0,
            InspectedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static InspectionSyncResponseDto CreateSampleSuccessResponse(string id = "insp-001")
    {
        return new InspectionSyncResponseDto(
            Success: true,
            Data: new InspectionSyncResultDto(
                Id: id,
                ShipmentId: "ship-100",
                Status: "SYNCED",
                SyncedAt: DateTime.UtcNow,
                AlertCreated: false
            ),
            Timestamp: DateTime.UtcNow
        );
    }

    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BackgroundSyncEngine(null!, _mockApiClient.Object, _mockConnectivityService.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WhenApiClientIsNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BackgroundSyncEngine(_mockRepository.Object, null!, _mockConnectivityService.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("apiClient");
    }

    [Fact]
    public void Constructor_WhenConnectivityServiceIsNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BackgroundSyncEngine(_mockRepository.Object, _mockApiClient.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectivityService");
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenNotConnected_ReturnsZeroWithoutCallingApi()
    {
        // Arrange
        _mockConnectivityService.Setup(c => c.IsConnected).Returns(false);

        // Act
        var result = await _syncEngine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        _mockRepository.Verify(r => r.GetPendingInspectionsAsync(), Times.Never);
        _mockApiClient.Verify(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenConnectedAndNoPendingRecords_ReturnsZero()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord>());

        // Act
        var result = await _syncEngine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        _mockRepository.Verify(r => r.GetPendingInspectionsAsync(), Times.Once);
        _mockApiClient.Verify(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenConnected_UploadsPendingRecordsAndMarksAsSynced()
    {
        // Arrange
        var record1 = CreateSampleRecord("insp-1", "ship-101");
        var record2 = CreateSampleRecord("insp-2", "ship-102");
        var pendingRecords = new List<LocalInspectionRecord> { record1, record2 };

        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(pendingRecords);

        var syncedTime1 = DateTime.UtcNow.AddSeconds(1);
        var syncedTime2 = DateTime.UtcNow.AddSeconds(2);

        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.Is<CreateInspectionRequestDto>(dto => dto.Id == "insp-1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InspectionSyncResponseDto(
                Success: true,
                Data: new InspectionSyncResultDto("insp-1", "ship-101", "SYNCED", syncedTime1, false),
                Timestamp: syncedTime1));

        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.Is<CreateInspectionRequestDto>(dto => dto.Id == "insp-2"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InspectionSyncResponseDto(
                Success: true,
                Data: new InspectionSyncResultDto("insp-2", "ship-102", "SYNCED", syncedTime2, false),
                Timestamp: syncedTime2));

        // Act
        var result = await _syncEngine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(2);
        _mockApiClient.Verify(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockRepository.Verify(r => r.MarkAsSyncedAsync("insp-1", syncedTime1), Times.Once);
        _mockRepository.Verify(r => r.MarkAsSyncedAsync("insp-2", syncedTime2), Times.Once);
        _mockRepository.Verify(r => r.MarkAsFailedAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenUploadFails_MarksRecordAsFailed()
    {
        // Arrange
        var record = CreateSampleRecord("insp-error-1", "ship-201");
        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { record });

        const string errorMessage = "HTTP 500: Database constraint violated";
        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException(errorMessage));

        // Act
        var result = await _syncEngine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        _mockApiClient.Verify(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.MarkAsFailedAsync("insp-error-1", errorMessage), Times.Once);
        _mockRepository.Verify(r => r.MarkAsSyncedAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenApiResponseIsNotSuccess_MarksRecordAsFailed()
    {
        // Arrange
        var record = CreateSampleRecord("insp-fail-api", "ship-202");
        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { record });

        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InspectionSyncResponseDto(
                Success: false,
                Data: null!,
                Timestamp: DateTime.UtcNow));

        // Act
        var result = await _syncEngine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        _mockRepository.Verify(r => r.MarkAsFailedAsync("insp-fail-api", It.Is<string>(s => s.Contains("failure"))), Times.Once);
        _mockRepository.Verify(r => r.MarkAsSyncedAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_FiresProgressEventsDuringSyncLoop()
    {
        // Arrange
        var record1 = CreateSampleRecord("insp-prog-1", "ship-301");
        var record2 = CreateSampleRecord("insp-prog-2", "ship-302");
        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { record1, record2 });

        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.Is<CreateInspectionRequestDto>(d => d.Id == "insp-prog-1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSuccessResponse("insp-prog-1"));

        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.Is<CreateInspectionRequestDto>(d => d.Id == "insp-prog-2"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var progressEvents = new List<SyncProgressEventArgs>();
        _syncEngine.SyncProgressChanged += (sender, args) => progressEvents.Add(args);

        // Act
        var result = await _syncEngine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(1);
        progressEvents.Should().HaveCount(2);

        // Event 1 (success)
        progressEvents[0].Total.Should().Be(2);
        progressEvents[0].Processed.Should().Be(1);
        progressEvents[0].Succeeded.Should().Be(1);
        progressEvents[0].Failed.Should().Be(0);
        progressEvents[0].Message.Should().Contain("insp-prog-1");

        // Event 2 (failure)
        progressEvents[1].Total.Should().Be(2);
        progressEvents[1].Processed.Should().Be(2);
        progressEvents[1].Succeeded.Should().Be(1);
        progressEvents[1].Failed.Should().Be(1);
        progressEvents[1].Message.Should().Contain("insp-prog-2");
        progressEvents[1].Message.Should().Contain("Timeout");
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenRepositoryThrows_ReportsProgressErrorAndReturnsZero()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ThrowsAsync(new InvalidOperationException("SQLite disk I/O error"));

        SyncProgressEventArgs? errorEvent = null;
        _syncEngine.SyncProgressChanged += (s, e) => errorEvent = e;

        // Act
        var result = await _syncEngine.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        errorEvent.Should().NotBeNull();
        errorEvent!.Message.Should().Contain("SQLite disk I/O error");
    }

    [Fact]
    public async Task ConnectivityChanged_WhenConnectedIsTrue_TriggersSync()
    {
        // Arrange
        var record = CreateSampleRecord("insp-conn-1", "ship-conn-1");
        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { record });

        var tcs = new TaskCompletionSource<bool>();
        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSuccessResponse("insp-conn-1"))
            .Callback(() => tcs.TrySetResult(true));

        // Act - raise event on mock connectivity
        _mockConnectivityService.Raise(c => c.ConnectivityChanged += null, _mockConnectivityService.Object, true);

        // Wait for async background sync
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));

        // Assert
        completed.Should().Be(tcs.Task, "SyncPendingInspectionsAsync should have been invoked on connectivity changed");
        _mockRepository.Verify(r => r.MarkAsSyncedAsync("insp-conn-1", It.IsAny<DateTime>()), Times.Once);
    }
}
