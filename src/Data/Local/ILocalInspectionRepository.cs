using CargoPulse.Mobile.Data.Local.Entities;

namespace CargoPulse.Mobile.Data.Local;

public interface ILocalInspectionRepository
{
    Task InitializeAsync();
    Task SaveInspectionAsync(LocalInspectionRecord inspection);
    Task<LocalInspectionRecord?> GetByIdAsync(string id);
    Task<List<LocalInspectionRecord>> GetPendingInspectionsAsync();
    Task<List<LocalInspectionRecord>> GetAllInspectionsAsync();
    Task MarkAsSyncedAsync(string id, DateTime syncedAt);
    Task MarkAsFailedAsync(string id, string error);
    Task<int> GetPendingCountAsync();
}
