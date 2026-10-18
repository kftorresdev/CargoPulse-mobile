using System.Text.Json;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Dtos;
using CargoPulse.Mobile.Data.Local;
using CargoPulse.Mobile.Data.Local.Entities;
using CargoPulse.Mobile.Data.Services;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class BackgroundSyncEngineTests : IDisposable
{
    private readonly Mock<ILocalInspectionRepository> _mockRepository;
    private readonly Mock<IInspectionApiClient> _mockApiClient;
    private readonly Mock<IConnectivityService> _mockConnectivityService;
    private readonly List<string> _tempFilesToCleanup = new();

    public BackgroundSyncEngineTests()
    {
        _mockRepository = new Mock<ILocalInspectionRepository>();
        _mockApiClient = new Mock<IInspectionApiClient>();
        _mockConnectivityService = new Mock<IConnectivityService>();

        _mockConnectivityService.Setup(c => c.IsConnected).Returns(true);
    }

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
                }
            }
        }
    }

    private BackgroundSyncEngine CreateSut()
    {
        return new BackgroundSyncEngine(
            _mockRepository.Object,
            _mockApiClient.Object,
            _mockConnectivityService.Object
        );
    }

    [Fact]
    public void Constructor_WhenDependenciesNull_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new BackgroundSyncEngine(null!, _mockApiClient.Object, _mockConnectivityService.Object));
        Assert.Throws<ArgumentNullException>(() => new BackgroundSyncEngine(_mockRepository.Object, null!, _mockConnectivityService.Object));
        Assert.Throws<ArgumentNullException>(() => new BackgroundSyncEngine(_mockRepository.Object, _mockApiClient.Object, null!));
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenNotConnected_ReturnsZeroAndDoesNotQuery()
    {
        // Arrange
        _mockConnectivityService.Setup(c => c.IsConnected).Returns(false);
        using var sut = CreateSut();

        // Act
        var result = await sut.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        _mockRepository.Verify(r => r.GetPendingInspectionsAsync(), Times.Never);
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenNoPendingInspections_ReturnsZero()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetPendingInspectionsAsync()).ReturnsAsync(new List<LocalInspectionRecord>());
        using var sut = CreateSut();

        // Act
        var result = await sut.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        sut.IsSyncing.Should().BeFalse();
        _mockApiClient.Verify(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenValidInspection_UploadsAndMarksAsSynced()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var pendingItem = new LocalInspectionRecord
        {
            Id = "insp-001",
            ShipmentId = "ship-100",
            ContainerCondition = "GOOD",
            SealIntact = true,
            DeclaredSealNumber = "SEAL-A",
            ActualSealNumber = "SEAL-A",
            SealMismatch = false,
            DamagesReported = false,
            PhotosJson = "[]",
            SignatureBase64 = "base64sig",
            SignerName = "Juan Lopez",
            GpsCoordinates = "19.4326,-99.1332",
            InspectedAt = now
        };

        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { pendingItem });

        var syncResult = new InspectionSyncResultDto("insp-001", "ship-100", "SYNCED", now, false);
        var syncResponse = new InspectionSyncResponseDto(true, syncResult, now);

        CreateInspectionRequestDto? capturedDto = null;
        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateInspectionRequestDto, CancellationToken>((dto, _) => capturedDto = dto)
            .ReturnsAsync(syncResponse);

        using var sut = CreateSut();
        var progressEvents = new List<SyncProgressEventArgs>();
        sut.SyncProgressChanged += (_, args) => progressEvents.Add(args);

        // Act
        var result = await sut.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(1);
        sut.IsSyncing.Should().BeFalse();

        capturedDto.Should().NotBeNull();
        capturedDto!.Id.Should().Be("insp-001");
        capturedDto.ShipmentId.Should().Be("ship-100");
        capturedDto.SealNumberDeclared.Should().Be("SEAL-A");
        capturedDto.SealNumberActual.Should().Be("SEAL-A");
        capturedDto.SignerName.Should().Be("Juan Lopez");

        _mockRepository.Verify(r => r.MarkAsSyncedAsync("insp-001", now), Times.Once);
        _mockRepository.Verify(r => r.MarkAsFailedAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        progressEvents.Should().HaveCount(1);
        progressEvents[0].Total.Should().Be(1);
        progressEvents[0].Processed.Should().Be(1);
        progressEvents[0].Succeeded.Should().Be(1);
        progressEvents[0].Failed.Should().Be(0);
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenUploadThrowsException_MarksAsFailed()
    {
        // Arrange
        var pendingItem = new LocalInspectionRecord
        {
            Id = "insp-002",
            ShipmentId = "ship-200",
            PhotosJson = "[]"
        };

        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { pendingItem });

        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Server error 500"));

        using var sut = CreateSut();
        var progressEvents = new List<SyncProgressEventArgs>();
        sut.SyncProgressChanged += (_, args) => progressEvents.Add(args);

        // Act
        var result = await sut.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(0);
        sut.IsSyncing.Should().BeFalse();

        _mockRepository.Verify(r => r.MarkAsFailedAsync("insp-002", "Server error 500"), Times.Once);
        _mockRepository.Verify(r => r.MarkAsSyncedAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);

        progressEvents.Should().HaveCount(1);
        progressEvents[0].Total.Should().Be(1);
        progressEvents[0].Processed.Should().Be(1);
        progressEvents[0].Succeeded.Should().Be(0);
        progressEvents[0].Failed.Should().Be(1);
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenPhotosOnDisk_ReadsAndEncodesToBase64Dto()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        _tempFilesToCleanup.Add(tempFile);
        var dummyBytes = new byte[] { 10, 20, 30, 40, 50 };
        await File.WriteAllBytesAsync(tempFile, dummyBytes);

        var photosJson = JsonSerializer.Serialize(new[] { tempFile });

        var pendingItem = new LocalInspectionRecord
        {
            Id = "insp-photos",
            ShipmentId = "ship-photo",
            PhotosJson = photosJson
        };

        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { pendingItem });

        CreateInspectionRequestDto? uploadedDto = null;
        var syncResponse = new InspectionSyncResponseDto(
            true,
            new InspectionSyncResultDto("insp-photos", "ship-photo", "SYNCED", DateTime.UtcNow, false),
            DateTime.UtcNow);

        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateInspectionRequestDto, CancellationToken>((dto, _) => uploadedDto = dto)
            .ReturnsAsync(syncResponse);

        using var sut = CreateSut();

        // Act
        var result = await sut.SyncPendingInspectionsAsync();

        // Assert
        result.Should().Be(1);
        uploadedDto.Should().NotBeNull();
        uploadedDto!.Photos.Should().HaveCount(1);
        uploadedDto.Photos[0].Base64Content.Should().Be(Convert.ToBase64String(dummyBytes));
        uploadedDto.Photos[0].FileName.Should().Be(Path.GetFileName(tempFile));
    }

    [Fact]
    public async Task ConnectivityChanged_WhenRaisedWithTrue_TriggersSync()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord>())
            .Callback(() => tcs.TrySetResult(true));

        using var sut = CreateSut();

        // Act
        _mockConnectivityService.Raise(c => c.ConnectivityChanged += null, _mockConnectivityService.Object, true);

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(1000));
        completed.Should().Be(tcs.Task);
        _mockRepository.Verify(r => r.GetPendingInspectionsAsync(), Times.Once);
    }

    [Fact]
    public async Task ConnectivityChanged_WhenRaisedWithFalse_DoesNotTriggerSync()
    {
        // Arrange
        using var sut = CreateSut();

        // Act
        _mockConnectivityService.Raise(c => c.ConnectivityChanged += null, _mockConnectivityService.Object, false);
        await Task.Delay(200);

        // Assert
        _mockRepository.Verify(r => r.GetPendingInspectionsAsync(), Times.Never);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimesSafely()
    {
        var sut = CreateSut();
        sut.Dispose();
        var act = () => sut.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenDisposed_ReturnsZeroImmediately()
    {
        var sut = CreateSut();
        sut.Dispose();

        var result = await sut.SyncPendingInspectionsAsync();
        result.Should().Be(0);
        _mockRepository.Verify(r => r.GetPendingInspectionsAsync(), Times.Never);
    }

    [Fact]
    public async Task SyncPendingInspectionsAsync_WhenPhotoZeroBytes_SkipsEmptyPhoto()
    {
        var tempFile = Path.GetTempFileName();
        _tempFilesToCleanup.Add(tempFile);
        await File.WriteAllBytesAsync(tempFile, Array.Empty<byte>());

        var photosJson = JsonSerializer.Serialize(new[] { tempFile });
        var pendingItem = new LocalInspectionRecord
        {
            Id = "insp-empty-photo",
            ShipmentId = "ship-photo-empty",
            PhotosJson = photosJson
        };

        _mockRepository.Setup(r => r.GetPendingInspectionsAsync())
            .ReturnsAsync(new List<LocalInspectionRecord> { pendingItem });

        CreateInspectionRequestDto? uploadedDto = null;
        var syncResponse = new InspectionSyncResponseDto(
            true,
            new InspectionSyncResultDto("insp-empty-photo", "ship-photo-empty", "SYNCED", DateTime.UtcNow, false),
            DateTime.UtcNow);

        _mockApiClient.Setup(a => a.UploadInspectionAsync(It.IsAny<CreateInspectionRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateInspectionRequestDto, CancellationToken>((dto, _) => uploadedDto = dto)
            .ReturnsAsync(syncResponse);

        using var sut = CreateSut();
        var result = await sut.SyncPendingInspectionsAsync();

        result.Should().Be(1);
        uploadedDto.Should().NotBeNull();
        uploadedDto!.Photos.Should().HaveCount(1);
        uploadedDto.Photos[0].Base64Content.Should().BeEmpty();
    }
}

