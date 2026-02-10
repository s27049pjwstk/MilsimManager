using MilsimManager.Models;

namespace MilsimManager.Services;

public interface IAwardService {
    Task<List<Award>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<Award?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Award> CreateAsync(string name, string? description, CancellationToken cancellationToken = default);
    Task<Award> UpdateAsync(int id, uint version, string name, string? description, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task AssignUserAsync(int awardId, int userId, string? comment, User approvedBy, CancellationToken cancellationToken = default);
    Task RemoveUserAsync(int awardId, int userId, CancellationToken cancellationToken = default);
}
