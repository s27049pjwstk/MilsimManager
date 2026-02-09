using MilsimManager.Models;

namespace MilsimManager.Services;

public interface ICertificationService {
    Task<List<Certification>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<Certification?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Certification> CreateAsync(string name, string? description, CancellationToken cancellationToken = default);
    Task<Certification> UpdateAsync(int id, uint version, string name, string? description, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task AssignUserAsync(int certificationId, int userId, string? comment, User approvedBy, CancellationToken cancellationToken = default);
    Task RemoveUserAsync(int certificationId, int userId, CancellationToken cancellationToken = default);
}
