using MilsimManager.Models;

namespace MilsimManager.Services;

public interface IUnitService {
    Task<Unit?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Unit?> GetByIdWithMembersAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Unit>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<Unit> CreateAsync(string name, string? abbreviation, string? description, CancellationToken cancellationToken = default);
    Task<Unit> UpdateAsync(int id, uint version, string name, string? abbreviation, string? description, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> AbbreviationExistsAsync(string? abbreviation, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> UnitExistsAsync(int id, CancellationToken cancellationToken = default);
}
