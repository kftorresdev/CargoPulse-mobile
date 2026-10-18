using CargoPulse.Mobile.Data.Local;
using CargoPulse.Mobile.Data.Local.Entities;
using FluentAssertions;

namespace CargoPulse.Mobile.Tests;

public class LocalInspectionRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly LocalInspectionRepository _repository;

    public LocalInspectionRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"cargopulse_test_{Guid.NewGuid():N}.db");
        _repository = new LocalInspectionRepository(_testDbPath);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }
        catch
        {
            // Ignore cleanup failure in temporary directory
        }
    }

    [Fact]
    public async Task InitializeAsync_CreatesTableSuccessfully()
    {
        // Act
        await _repository.InitializeAsync();

        // Assert
        var count = await _repository.GetPendingCountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SaveInspectionAsync_InsertsNewRecord()
    {
        // Arrange
        var record = new LocalInspectionRecord
        {
            Id = Guid.NewGuid().ToString(),
            ShipmentId = "SHIP-100",
            TrackingNumber = "TRK-001",
            SyncStatus = "PENDING",
            ContainerCondition = "GOOD",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.SaveInspectionAsync(record);
        var retrieved = await _repository.GetByIdAsync(record.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(record.Id);
        retrieved.ShipmentId.Should().Be("SHIP-100");
        retrieved.TrackingNumber.Should().Be("TRK-001");
        retrieved.SyncStatus.Should().Be("PENDING");
    }

    [Fact]
    public async Task SaveInspectionAsync_UpdatesExistingRecord()
    {
        // Arrange
        var record = new LocalInspectionRecord
        {
            Id = Guid.NewGuid().ToString(),
            ShipmentId = "SHIP-100",
            ContainerCondition = "GOOD",
            SyncStatus = "PENDING"
        };
        await _repository.SaveInspectionAsync(record);

        // Act
        record.ContainerCondition = "DAMAGED";
        record.DamagesReported = true;
        record.DamagesDescription = "Punctured wall";
        await _repository.SaveInspectionAsync(record);

        var updated = await _repository.GetByIdAsync(record.Id);

        // Assert
        updated.Should().NotBeNull();
        updated!.ContainerCondition.Should().Be("DAMAGED");
        updated.DamagesReported.Should().BeTrue();
        updated.DamagesDescription.Should().Be("Punctured wall");
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidOrNotFoundId_ReturnsNull()
    {
        // Act
        var nullResult = await _repository.GetByIdAsync(string.Empty);
        var notFoundResult = await _repository.GetByIdAsync("non-existent-id");

        // Assert
        nullResult.Should().BeNull();
        notFoundResult.Should().BeNull();
    }

    [Fact]
    public async Task GetPendingInspectionsAsync_ReturnsOnlyPendingOrderedByCreatedAtAsc()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var record1 = new LocalInspectionRecord
        {
            Id = "1",
            SyncStatus = "PENDING",
            CreatedAt = now.AddMinutes(10)
        };
        var record2 = new LocalInspectionRecord
        {
            Id = "2",
            SyncStatus = "SYNCED",
            CreatedAt = now.AddMinutes(5)
        };
        var record3 = new LocalInspectionRecord
        {
            Id = "3",
            SyncStatus = "PENDING",
            CreatedAt = now.AddMinutes(1)
        };

        await _repository.SaveInspectionAsync(record1);
        await _repository.SaveInspectionAsync(record2);
        await _repository.SaveInspectionAsync(record3);

        // Act
        var pending = await _repository.GetPendingInspectionsAsync();

        // Assert
        pending.Should().HaveCount(2);
        pending[0].Id.Should().Be("3");
        pending[1].Id.Should().Be("1");
    }

    [Fact]
    public async Task GetAllInspectionsAsync_ReturnsAllOrderedByCreatedAtDesc()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var record1 = new LocalInspectionRecord { Id = "1", CreatedAt = now.AddMinutes(1) };
        var record2 = new LocalInspectionRecord { Id = "2", CreatedAt = now.AddMinutes(10) };
        var record3 = new LocalInspectionRecord { Id = "3", CreatedAt = now.AddMinutes(5) };

        await _repository.SaveInspectionAsync(record1);
        await _repository.SaveInspectionAsync(record2);
        await _repository.SaveInspectionAsync(record3);

        // Act
        var all = await _repository.GetAllInspectionsAsync();

        // Assert
        all.Should().HaveCount(3);
        all[0].Id.Should().Be("2");
        all[1].Id.Should().Be("3");
        all[2].Id.Should().Be("1");
    }

    [Fact]
    public async Task MarkAsSyncedAsync_UpdatesSyncStatusAndTimestampAndClearsError()
    {
        // Arrange
        var record = new LocalInspectionRecord
        {
            Id = Guid.NewGuid().ToString(),
            SyncStatus = "FAILED",
            LastError = "Network timeout",
            RetryCount = 2
        };
        await _repository.SaveInspectionAsync(record);

        var syncedAt = DateTime.UtcNow;

        // Act
        await _repository.MarkAsSyncedAsync(record.Id, syncedAt);
        var result = await _repository.GetByIdAsync(record.Id);

        // Assert
        result.Should().NotBeNull();
        result!.SyncStatus.Should().Be("SYNCED");
        result.SyncedAt.Should().BeCloseTo(syncedAt, TimeSpan.FromSeconds(1));
        result.LastError.Should().BeNull();
        result.RetryCount.Should().Be(2); // RetryCount remains unchanged on sync
    }

    [Fact]
    public async Task MarkAsFailedAsync_UpdatesSyncStatusAndErrorAndIncrementsRetryCount()
    {
        // Arrange
        var record = new LocalInspectionRecord
        {
            Id = Guid.NewGuid().ToString(),
            SyncStatus = "PENDING",
            RetryCount = 1,
            LastError = null
        };
        await _repository.SaveInspectionAsync(record);

        // Act
        await _repository.MarkAsFailedAsync(record.Id, "400 Bad Request");
        var result = await _repository.GetByIdAsync(record.Id);

        // Assert
        result.Should().NotBeNull();
        result!.SyncStatus.Should().Be("FAILED");
        result.LastError.Should().Be("400 Bad Request");
        result.RetryCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPendingCountAsync_ReturnsAccurateCount()
    {
        // Arrange
        await _repository.SaveInspectionAsync(new LocalInspectionRecord { Id = "1", SyncStatus = "PENDING" });
        await _repository.SaveInspectionAsync(new LocalInspectionRecord { Id = "2", SyncStatus = "PENDING" });
        await _repository.SaveInspectionAsync(new LocalInspectionRecord { Id = "3", SyncStatus = "SYNCED" });
        await _repository.SaveInspectionAsync(new LocalInspectionRecord { Id = "4", SyncStatus = "FAILED" });

        // Act
        var count = await _repository.GetPendingCountAsync();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task Operations_WithoutExplicitInitializeAsync_LazilyInitializes()
    {
        // Arrange
        var freshDbPath = Path.Combine(Path.GetTempPath(), $"cargopulse_lazy_{Guid.NewGuid():N}.db");
        var freshRepo = new LocalInspectionRepository(freshDbPath);

        try
        {
            // Act
            var count = await freshRepo.GetPendingCountAsync();

            // Assert
            count.Should().Be(0);
        }
        finally
        {
            try
            {
                if (File.Exists(freshDbPath))
                {
                    File.Delete(freshDbPath);
                }
            }
            catch
            {
            }
        }
    }
}
