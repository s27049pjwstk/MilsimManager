using MilsimManager.Models;

namespace MilsimManager.Services;

public interface IRankService {
    Task<List<Rank>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<List<RankLog>> GetRecentChangesAsync(CancellationToken cancellationToken = default);
    Task<Rank?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Rank> CreateAsync(string name, string? abbreviation, string? code, string? description, CancellationToken cancellationToken = default);
    Task<Rank> UpdateAsync(int id, uint version, string name, string? abbreviation, string? code, string? description, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string? code, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> AbbreviationExistsAsync(string? abbreviation, int? excludeId = null, CancellationToken cancellationToken = default);
    Task UpdateSortOrderAsync(IReadOnlyList<int> orderedRankIds, CancellationToken cancellationToken = default);
}
