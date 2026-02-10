using MilsimManager.Models;

namespace MilsimManager.Services;

public interface IEventService {
    Task<List<Event>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Event> CreateAsync(string name, string? description, DateTime date, CancellationToken cancellationToken = default);
    Task<Event> UpdateAsync(int id, uint version, string name, string? description, DateTime date, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task AttendAsync(int eventId, int userId, CancellationToken cancellationToken = default);
    Task UnattendAsync(int eventId, int userId, CancellationToken cancellationToken = default);
}
