using Microsoft.EntityFrameworkCore;
using MilsimManager.Models;

namespace MilsimManager.Services;

public class UserService(IDbContextFactory<Context> dbFactory) : IUserService {
    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

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
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

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
            .ToListAsync(cancellationToken);
    }

    public async Task<uint> UpdateAssignmentAsync(
        int userId,
        uint version,
        int? unitId,
        string? unitRole,
        User approvedBy,
        CancellationToken cancellationToken = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) throw new AppException("User not found");
        db.Entry(user).Property(u => u.Version).OriginalValue = version;

        unitRole = string.IsNullOrWhiteSpace(unitRole) ? null : unitRole.Trim();
        if (unitRole is not null && unitRole.Length > 64)
            throw new AppException("Unit role must be at most 64 characters");

        if (user.UnitId == unitId && user.UnitRole == unitRole) return user.Version;

        Unit? unit = null;
        if (unitId is not null) {
            unit = await db.Units.SingleOrDefaultAsync(u => u.Id == unitId, cancellationToken);
            if (unit is null) throw new AppException("Unit not found");
        }

        user.UnitId = unitId;
        user.UnitRole = unitRole;
        db.UnitAssignmentLogs.Add(new UnitAssignmentLog {
            User = user,
            Unit = unit,
            UnitName = unit?.Name ?? "None",
            UnitAbbreviation = unit?.Abbreviation ?? string.Empty,
            Role = unitRole ?? string.Empty,
            ApprovedById = approvedBy.Id,
            ApprovedByName = approvedBy.Name
        });

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }
        return user.Version;
    }

    public async Task<uint> UpdateNoteAsync(int userId, uint version, string? note, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) throw new AppException("User not found");
        db.Entry(user).Property(u => u.Version).OriginalValue = version;

        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note is not null && note.Length > 1000)
            throw new AppException("Note must be at most 1000 characters");
        if (user.Note == note) return user.Version;
        user.Note = note;

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }
        return user.Version;
    }

    public async Task<uint> UpdateRankAsync(
        int userId,
        uint version,
        int? rankId,
        User approvedBy,
        CancellationToken cancellationToken = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) throw new AppException("User not found");
        db.Entry(user).Property(u => u.Version).OriginalValue = version;

        Rank? rank = null;
        if (rankId is not null) {
            rank = await db.Ranks.SingleOrDefaultAsync(r => r.Id == rankId, cancellationToken);
            if (rank is null) throw new AppException("Rank not found");
        }

        if (user.RankId == rankId) return user.Version;

        user.RankId = rankId;
        db.RankLogs.Add(new RankLog {
            User = user,
            Rank = rank,
            RankName = rank?.Name ?? "No rank",
            ApprovedById = approvedBy.Id,
            ApprovedByName = approvedBy.Name
        });

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }
        return user.Version;
    }

    public async Task<uint> UpdateSteamIdAsync(int userId, uint version, string? steamId, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) throw new AppException("User not found");
        db.Entry(user).Property(u => u.Version).OriginalValue = version;

        steamId = string.IsNullOrWhiteSpace(steamId) ? null : steamId.Trim();
        if (user.SteamId == steamId) return user.Version;

        if (!string.IsNullOrWhiteSpace(steamId)) {
            if (steamId.Length != 17 || !steamId.All(char.IsDigit))
                throw new AppException("SteamID64 must be 17 digits");
            var inUse = await db.Users.AsNoTracking()
                .AnyAsync(u => u.SteamId == steamId && u.Id != userId, cancellationToken);
            if (inUse) throw new AppException("SteamID is already in use");
        }

        user.SteamId = steamId;

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return user.Version;
    }

    public async Task<uint> UpdateStatusAsync(
        int userId,
        uint version,
        bool active,
        User approvedBy,
        CancellationToken cancellationToken = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) throw new AppException("User not found");
        db.Entry(user).Property(u => u.Version).OriginalValue = version;

        if (user.Active == active) return user.Version;
        user.Active = active;

        db.StatusLogs.Add(new StatusLog {
            User = user,
            Status = active,
            ApprovedById = approvedBy.Id,
            ApprovedByName = approvedBy.Name
        });

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }
        return user.Version;
    }

    public async Task<uint> AddLeaveOfAbsenceAsync(
        int userId,
        uint version,
        DateTime? dateStart,
        DateTime dateEnd,
        CancellationToken cancellationToken = default
    ) {
        dateEnd = dateEnd.ToUniversalTime();
        dateStart = dateStart?.ToUniversalTime();
        var now = DateTime.UtcNow;
        if (dateStart <= now) dateStart = now;
        var dateStartNonNull = dateStart ?? now;
        if (dateEnd < dateStartNonNull) throw new AppException("End date must be in the future and after start date");

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var hasActiveLoa = await db.LeaveOfAbsences
            .AsNoTracking()
            .AnyAsync(l => l.UserId == userId && l.DateEnd >= now, cancellationToken);
        if (hasActiveLoa) throw new AppException("Only one active leave of absence is allowed");

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) throw new AppException("User not found");
        db.Entry(user).Property(u => u.Version).OriginalValue = version;

        db.LeaveOfAbsences.Add(new LeaveOfAbsence {
            User = user,
            DateStart = dateStartNonNull,
            DateEnd = dateEnd
        });

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }
        return user.Version;
    }
}
