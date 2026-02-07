using Microsoft.EntityFrameworkCore;
using MilsimManager.Models;

namespace MilsimManager.Services;

public class UnitService(IDbContextFactory<Context> dbFactory) : IUnitService {
    public async Task<Unit?> GetByIdAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<Unit?> GetByIdWithMembersAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Units
            .AsNoTracking()
            .Include(u => u.Users)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<List<Unit>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Units
            .AsNoTracking()
            .Include(u => u.Users)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search)) {
            search = search.Trim().ToLower();
            q = q.Where(u =>
                u.Name.ToLower().Contains(search) ||
                (u.Abbreviation != null && u.Abbreviation.ToLower().Contains(search)));
        }

        return await q
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Unit> CreateAsync(string name, string? abbreviation, string? description, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var unit = new Unit {
            Name = name.Trim(),
            Abbreviation = string.IsNullOrWhiteSpace(abbreviation) ? null : abbreviation.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

        db.Units.Add(unit);

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return unit;
    }

    public async Task<Unit> UpdateAsync(int id, uint version, string name, string? abbreviation, string? description, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        Unit unit;
        try {
            unit = await db.Units.SingleAsync(u => u.Id == id, cancellationToken);
        } catch (InvalidOperationException) {
            throw new AppException("Unit not found");
        }
        db.Entry(unit).Property(u => u.Version).OriginalValue = version;

        unit.Name = name.Trim();
        unit.Abbreviation = string.IsNullOrWhiteSpace(abbreviation) ? null : abbreviation.Trim();
        unit.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return unit;
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var deleted = await db.Units.Where(u => u.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? throw new AppException("Unit not found") : deleted;
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Units.AsNoTracking().Where(u => u.Name == name);
        if (excludeId is not null) q = q.Where(u => u.Id != excludeId.Value);
        return await q.AnyAsync(cancellationToken);
    }

    public async Task<bool> AbbreviationExistsAsync(string? abbreviation, int? excludeId = null, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(abbreviation))
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var abbr = abbreviation.Trim();
        var q = db.Units.AsNoTracking().Where(u => u.Abbreviation == abbr);
        if (excludeId is not null) q = q.Where(u => u.Id != excludeId.Value);
        return await q.AnyAsync(cancellationToken);
    }

    public async Task<bool> UnitExistsAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Units.AsNoTracking().AnyAsync(u => u.Id == id, cancellationToken);
    }
}
