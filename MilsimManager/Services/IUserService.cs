using MilsimManager.Models;

namespace MilsimManager.Services;

public interface IUserService {
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<uint> UpdateAssignmentAsync(int userId, uint version, int? unitId, string? unitRole, User approvedBy, CancellationToken cancellationToken = default);
    Task<uint> UpdateNoteAsync(int userId, uint version, string? note, CancellationToken cancellationToken = default);
    Task<uint> UpdateRankAsync(int userId, uint version, int? rankId, User approvedBy, CancellationToken cancellationToken = default);
    Task<uint> UpdateSteamIdAsync(int userId, uint version, string? steamId, CancellationToken cancellationToken = default);
    Task<uint> UpdateStatusAsync(int userId, uint version, bool active, User approvedBy, CancellationToken cancellationToken = default);
    Task<uint> AddLeaveOfAbsenceAsync(int userId, uint version, DateTime? dateStart, DateTime dateEnd, CancellationToken cancellationToken = default);

}
