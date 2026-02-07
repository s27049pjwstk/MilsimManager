using Microsoft.EntityFrameworkCore;
using MilsimManager.Models;

namespace MilsimManager.Services;

public class UnitService(IDbContextFactory<Context> dbFactory) : IUnitService {
    public async Task<Unit?> GetByIdAsync(int id) {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Unit?> GetByIdWithMembersAsync(int id) {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Units
            .AsNoTracking()
            .Include(u => u.Users)
            .FirstOrDefaultAsync(u => u.Id == id);
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

    public async Task<Unit> CreateAsync(string name, string? abbreviation, string? description) {
        await using var db = await dbFactory.CreateDbContextAsync();

        var unit = new Unit {
            Name = name.Trim(),
            Abbreviation = string.IsNullOrWhiteSpace(abbreviation) ? null : abbreviation.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

        db.Units.Add(unit);

        try {
            await db.SaveChangesAsync();
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return unit;
    }

    public async Task<Unit> UpdateAsync(int id, uint version, string name, string? abbreviation, string? description) {
        await using var db = await dbFactory.CreateDbContextAsync();

        Unit unit;
        try {
            unit = await db.Units.SingleAsync(u => u.Id == id);
        } catch (InvalidOperationException) {
            throw new AppException("Unit not found");
        }
        db.Entry(unit).Property(u => u.Version).OriginalValue = version;

        unit.Name = name.Trim();
        unit.Abbreviation = string.IsNullOrWhiteSpace(abbreviation) ? null : abbreviation.Trim();
        unit.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        try {
            await db.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return unit;
    }

    public async Task<int> DeleteAsync(int id) {
        await using var db = await dbFactory.CreateDbContextAsync();

        var deleted = await db.Units.Where(u => u.Id == id).ExecuteDeleteAsync();
        return deleted == 0 ? throw new AppException("Unit not found") : deleted;
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null) {
        await using var db = await dbFactory.CreateDbContextAsync();

        var q = db.Units.AsNoTracking().Where(u => u.Name == name);
        if (excludeId is not null) q = q.Where(u => u.Id != excludeId.Value);
        return await q.AnyAsync();
    }

    public async Task<bool> AbbreviationExistsAsync(string? abbreviation, int? excludeId = null) {
        if (string.IsNullOrWhiteSpace(abbreviation))
            return false;

        await using var db = await dbFactory.CreateDbContextAsync();

        var abbr = abbreviation.Trim();
        var q = db.Units.AsNoTracking().Where(u => u.Abbreviation == abbr);
        if (excludeId is not null) q = q.Where(u => u.Id != excludeId.Value);
        return await q.AnyAsync();
    }

    public async Task<bool> UnitExists(int id) {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Units.AsNoTracking().AnyAsync(u => u.Id == id);
    }
}
