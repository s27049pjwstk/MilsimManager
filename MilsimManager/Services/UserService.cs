using Microsoft.EntityFrameworkCore;
using MilsimManager.Models;

namespace MilsimManager.Services;

public class UserService(IDbContextFactory<Context> dbFactory) : IUserService {
    public async Task<User?> GetByIdAsync(int id) {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.Users
            .AsNoTracking()
            .Include(u => u.Rank)
            .Include(u => u.Unit)
            .Include(u => u.LeaveOfAbsences)
            .Include(u => u.StatusLogs)
            .Include(u => u.RankLogs).ThenInclude(l => l.Rank)
            .Include(u => u.UnitAssignmentLogs).ThenInclude(l => l.Unit)
            .Include(u => u.UserAwards).ThenInclude(ua => ua.Award)
            .Include(u => u.UserCertifications).ThenInclude(uc => uc.Certification)
            .Include(u => u.UserAttendances).ThenInclude(ua => ua.Event)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<User>> GetAllAsync(string? search = null) {
        await using var db = await dbFactory.CreateDbContextAsync();

        var q = db.Users
            .AsNoTracking()
            .Include(u => u.Rank)
            .Include(u => u.Unit)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search)) {
            search = search.Trim().ToLower();
            q = q.Where(u => u.Name.ToLower().Contains(search));
        }

        return await q
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task<bool> UserExists(int id) {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().AnyAsync(u => u.Id == id);
    }

    public async Task<User> AssignUserAsync(int userId, uint version, int? unitId, string? unitRole) {
        await using var db = await dbFactory.CreateDbContextAsync();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) throw new AppException("User not found");
        db.Entry(user).Property(u => u.Version).OriginalValue = version;

        if (unitId is not null) {
            var unitExists = await db.Units.AsNoTracking().AnyAsync(u => u.Id == unitId);
            if (!unitExists) throw new AppException("Unit not found");
            user.UnitId = unitId;
        } else {
            user.UnitId = null;
        }

        user.UnitRole = string.IsNullOrWhiteSpace(unitRole) ? null : unitRole.Trim();

        try {
            await db.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        }
        return user;
    }
}