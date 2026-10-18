using System.Collections.ObjectModel;
using CargoPulse.Mobile.Core.Services;
using CargoPulse.Mobile.Data.Local;
using CargoPulse.Mobile.Data.Local.Entities;
using CargoPulse.Mobile.ViewModels;
using FluentAssertions;
using Moq;

namespace CargoPulse.Mobile.Tests;

public class SyncQueueViewModelTests
{
    private readonly Mock<ILocalInspectionRepository> _mockRepo;
    private readonly Mock<ISyncEngine> _mockSyncEngine;
    private readonly Mock<IConnectivityService> _mockConnectivity;
    private readonly SyncQueueViewModel _sut;

    public SyncQueueViewModelTests()
    {
        _mockRepo = new Mock<ILocalInspectionRepository>();
        _mockSyncEngine = new Mock<ISyncEngine>();
        _mockConnectivity = new Mock<IConnectivityService>();

        _mockConnectivity.Setup(c => c.IsConnected).Returns(true);
        _mockSyncEngine.Setup(s => s.IsSyncing).Returns(false);

        _sut = new SyncQueueViewModel(
            _mockRepo.Object,
            _mockSyncEngine.Object,
            _mockConnectivity.Object
        );
    }

    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        _sut.Inspections.Should().BeEmpty();
        _sut.PendingCount.Should().Be(0);
        _sut.IsOnline.Should().BeTrue();
        _sut.IsSyncing.Should().BeFalse();
        _sut.SyncMessage.Should().BeEmpty();
        _sut.ProgressPercent.Should().Be(0);
        _sut.IsEmpty.Should().BeTrue();
        _sut.Title.Should().Be("Cola de Sincronización");
    }

    [Fact]
    public void Constructor_WhenDependenciesNull_ThrowsArgumentNullException()
    {
        Action act1 = () => new SyncQueueViewModel(null!, _mockSyncEngine.Object, _mockConnectivity.Object);
        Action act2 = () => new SyncQueueViewModel(_mockRepo.Object, null!, _mockConnectivity.Object);
        Action act3 = () => new SyncQueueViewModel(_mockRepo.Object, _mockSyncEngine.Object, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task RefreshCommand_LoadsInspectionsAndUpdatesCountsAndIsEmpty()
    {
        var records = new List<LocalInspectionRecord>
        {
            new() { Id = "rec-1", TrackingNumber = "TRK-001", SyncStatus = "PENDING" },
            new() { Id = "rec-2", TrackingNumber = "TRK-002", SyncStatus = "SYNCED" }
        };

        _mockRepo.Setup(r => r.GetAllInspectionsAsync()).ReturnsAsync(records);
        _mockRepo.Setup(r => r.GetPendingCountAsync()).ReturnsAsync(1);

        await _sut.RefreshCommand.ExecuteAsync(null);

        _sut.Inspections.Should().HaveCount(2);
        _sut.PendingCount.Should().Be(1);
        _sut.IsEmpty.Should().BeFalse();
        _mockRepo.Verify(r => r.GetAllInspectionsAsync(), Times.Once);
        _mockRepo.Verify(r => r.GetPendingCountAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshCommand_WhenNoInspections_SetsIsEmptyTrue()
    {
        _mockRepo.Setup(r => r.GetAllInspectionsAsync()).ReturnsAsync(new List<LocalInspectionRecord>());
        _mockRepo.Setup(r => r.GetPendingCountAsync()).ReturnsAsync(0);

        await _sut.RefreshCommand.ExecuteAsync(null);

        _sut.Inspections.Should().BeEmpty();
        _sut.PendingCount.Should().Be(0);
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task ForceSyncCommand_ExecutesSyncEngineAndRefreshesQueue()
    {
        var syncedRecords = new List<LocalInspectionRecord>
        {
            new() { Id = "rec-1", TrackingNumber = "TRK-001", SyncStatus = "SYNCED" }
        };

        _mockSyncEngine
            .Setup(s => s.SyncPendingInspectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockRepo.Setup(r => r.GetAllInspectionsAsync()).ReturnsAsync(syncedRecords);
        _mockRepo.Setup(r => r.GetPendingCountAsync()).ReturnsAsync(0);

        await _sut.ForceSyncCommand.ExecuteAsync(null);

        _mockSyncEngine.Verify(s => s.SyncPendingInspectionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sut.Inspections.Should().HaveCount(1);
        _sut.PendingCount.Should().Be(0);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsSyncing.Should().BeFalse();
    }

    [Fact]
    public async Task ForceSyncCommand_WhenOffline_SetsInformativeMessage()
    {
        _mockConnectivity.Setup(c => c.IsConnected).Returns(false);
        _mockRepo.Setup(r => r.GetAllInspectionsAsync()).ReturnsAsync(new List<LocalInspectionRecord>());
        _mockRepo.Setup(r => r.GetPendingCountAsync()).ReturnsAsync(0);

        await _sut.ForceSyncCommand.ExecuteAsync(null);

        _sut.SyncMessage.Should().Contain("offline");
    }

    [Fact]
    public void SyncProgressChanged_UpdatesMessageAndProgressPercent()
    {
        _mockSyncEngine.Raise(s => s.SyncProgressChanged += null, new SyncProgressEventArgs
        {
            Total = 4,
            Processed = 2,
            Succeeded = 2,
            Failed = 0,
            Message = "Sincronizando 2 de 4"
        });

        _sut.SyncMessage.Should().Be("Sincronizando 2 de 4");
        _sut.ProgressPercent.Should().Be(0.5);
    }

    [Fact]
    public void ConnectivityChanged_UpdatesIsOnlineProperty()
    {
        _mockConnectivity.Raise(c => c.ConnectivityChanged += null, _mockConnectivity.Object, false);
        _sut.IsOnline.Should().BeFalse();

        _mockConnectivity.Raise(c => c.ConnectivityChanged += null, _mockConnectivity.Object, true);
        _sut.IsOnline.Should().BeTrue();
    }

    [Fact]
    public void Dispose_UnsubscribesFromEventsAndIsIdempotent()
    {
        // Act - Dispose once
        _sut.Dispose();

        // Calling Dispose again should not throw
        var act = () => _sut.Dispose();
        act.Should().NotThrow();

        // Raise events - should not update view model properties
        _mockConnectivity.Raise(c => c.ConnectivityChanged += null, _mockConnectivity.Object, false);
        _sut.IsOnline.Should().BeTrue();

        _mockSyncEngine.Raise(s => s.SyncProgressChanged += null, new SyncProgressEventArgs
        {
            Total = 10,
            Processed = 5,
            Message = "Test message after dispose"
        });
        _sut.SyncMessage.Should().BeEmpty();
        _sut.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public async Task Commands_WhenDisposed_DoNotExecute()
    {
        _sut.Dispose();

        await _sut.RefreshCommand.ExecuteAsync(null);
        _mockRepo.Verify(r => r.GetAllInspectionsAsync(), Times.Never);

        await _sut.ForceSyncCommand.ExecuteAsync(null);
        _mockSyncEngine.Verify(s => s.SyncPendingInspectionsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForceSyncCommand_WhenFailedInspectionsExist_ResetsThemToPendingAndClearsLastError()
    {
        var failedRecord = new LocalInspectionRecord
        {
            Id = "rec-failed-1",
            TrackingNumber = "TRK-ERR-001",
            SyncStatus = "FAILED",
            LastError = "uploading inspection failed with HTTP 500 success...",
            RetryCount = 1
        };

        var allRecords = new List<LocalInspectionRecord> { failedRecord };
        _mockRepo.Setup(r => r.GetAllInspectionsAsync()).ReturnsAsync(allRecords);
        _mockRepo.Setup(r => r.GetPendingCountAsync()).ReturnsAsync(1);
        _mockSyncEngine
            .Setup(s => s.SyncPendingInspectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        LocalInspectionRecord? savedRecord = null;
        _mockRepo.Setup(r => r.SaveInspectionAsync(It.IsAny<LocalInspectionRecord>()))
            .Callback<LocalInspectionRecord>(rec => savedRecord = rec)
            .Returns(Task.CompletedTask);

        await _sut.ForceSyncCommand.ExecuteAsync(null);

        _mockRepo.Verify(r => r.SaveInspectionAsync(It.IsAny<LocalInspectionRecord>()), Times.Once);
        savedRecord.Should().NotBeNull();
        savedRecord!.SyncStatus.Should().Be("PENDING");
        savedRecord.LastError.Should().BeNull();
        _mockSyncEngine.Verify(s => s.SyncPendingInspectionsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForceSyncCommand_ClearsPreviousSyncMessageError()
    {
        _sut.SyncMessage = "Error durante la sincronización: Transient failure";
        _mockRepo.Setup(r => r.GetAllInspectionsAsync()).ReturnsAsync(new List<LocalInspectionRecord>());
        _mockRepo.Setup(r => r.GetPendingCountAsync()).ReturnsAsync(0);
        _mockSyncEngine
            .Setup(s => s.SyncPendingInspectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.ForceSyncCommand.ExecuteAsync(null);

        _sut.SyncMessage.Should().NotContain("Transient failure");
    }

    [Fact]
    public async Task ShowErrorDetailsCommand_InvokesDisplayAlertFuncWithFullErrorTextAndRetryCount()
    {
        string? capturedTitle = null;
        string? capturedMessage = null;
        string? capturedCancel = null;

        _sut.DisplayAlertFunc = (title, message, cancel) =>
        {
            capturedTitle = title;
            capturedMessage = message;
            capturedCancel = cancel;
            return Task.CompletedTask;
        };

        var record = new LocalInspectionRecord
        {
            Id = "rec-fail-details",
            TrackingNumber = "TRK-LONG-ERROR",
            SyncStatus = "FAILED",
            LastError = "uploading inspection failed with HTTP 500 success false: Server database timeout occurred during processing.",
            RetryCount = 3
        };

        await _sut.ShowErrorDetailsCommand.ExecuteAsync(record);

        capturedTitle.Should().Contain("TRK-LONG-ERROR");
        capturedMessage.Should().Contain("Server database timeout occurred during processing");
        capturedMessage.Should().Contain("Reintentos realizados: 3");
        capturedCancel.Should().Be("Cerrar");
    }

    [Fact]
    public async Task ShowErrorDetailsCommand_WhenRecordIsNull_DoesNotThrow()
    {
        var act = async () => await _sut.ShowErrorDetailsCommand.ExecuteAsync(null);
        await act.Should().NotThrowAsync();
    }
}

