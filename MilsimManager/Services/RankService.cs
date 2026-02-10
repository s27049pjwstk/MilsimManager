using Microsoft.EntityFrameworkCore;
using MilsimManager.Models;

namespace MilsimManager.Services;

public class RankService(IDbContextFactory<Context> dbFactory) : IRankService {
    public async Task<List<Rank>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Ranks.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search)) {
            search = search.Trim().ToLower();
            q = q.Where(r =>
                r.Name.ToLower().Contains(search) ||
                (r.Abbreviation != null && r.Abbreviation.ToLower().Contains(search)) ||
                (r.Code != null && r.Code.ToLower().Contains(search)) ||
                (r.Description != null && r.Description.ToLower().Contains(search))
            );
        }

        return await q
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Rank?> GetByIdAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Ranks
            .AsNoTracking()
            .Include(r => r.Users)
            .ThenInclude(u => u.Unit)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Rank> CreateAsync(
        string name,
        string? abbreviation,
        string? code,
        string? description,
        CancellationToken cancellationToken = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        name = string.IsNullOrEmpty(name) ? throw new AppException("Name is required") : name.Trim();
        abbreviation = string.IsNullOrWhiteSpace(abbreviation) ? null : abbreviation.Trim();
        code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (await NameExistsAsync(name, null, cancellationToken))
            throw new AppException("A rank with this name already exists");
        if (await AbbreviationExistsAsync(abbreviation, null, cancellationToken))
            throw new AppException("A rank with this abbreviation already exists");
        if (await CodeExistsAsync(code, null, cancellationToken))
            throw new AppException("A rank with this code already exists");

        var maxSortOrder = await db.Ranks
            .Select(r => (int?)r.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var rank = new Rank {
            Name = name,
            Abbreviation = abbreviation,
            Code = code,
            Description = description,
            SortOrder = maxSortOrder + 1
        };

        db.Ranks.Add(rank);

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return rank;
    }

    public async Task<Rank> UpdateAsync(
        int id,
        uint version,
        string name,
        string? abbreviation,
        string? code,
        string? description,
        CancellationToken cancellationToken = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        Rank rank;
        try {
            rank = await db.Ranks.SingleAsync(r => r.Id == id, cancellationToken);
        } catch (InvalidOperationException) {
            throw new AppException("Rank not found");
        }
        db.Entry(rank).Property(r => r.Version).OriginalValue = version;

        name = string.IsNullOrEmpty(name) ? throw new AppException("Name is required") : name.Trim();
        abbreviation = string.IsNullOrWhiteSpace(abbreviation) ? null : abbreviation.Trim();
        code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (await NameExistsAsync(name, id, cancellationToken))
            throw new AppException("A rank with this name already exists");
        if (await AbbreviationExistsAsync(abbreviation, id, cancellationToken))
            throw new AppException("A rank with this abbreviation already exists");
        if (await CodeExistsAsync(code, id, cancellationToken))
            throw new AppException("A rank with this code already exists");

        rank.Name = name;
        rank.Abbreviation = abbreviation;
        rank.Code = code;
        rank.Description = description;

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return rank;
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var deleted = await db.Ranks.Where(r => r.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? throw new AppException("Rank not found") : deleted;
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Ranks.AsNoTracking().Where(r => r.Name == name.Trim());
        if (excludeId is not null) q = q.Where(r => r.Id != excludeId.Value);
        return await q.AnyAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string? code, int? excludeId = null, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(code)) return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Ranks.AsNoTracking().Where(r => r.Code == code.Trim());
        if (excludeId is not null) q = q.Where(r => r.Id != excludeId.Value);
        return await q.AnyAsync(cancellationToken);
    }

    public async Task<bool> AbbreviationExistsAsync(string? abbreviation, int? excludeId = null, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(abbreviation)) return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Ranks.AsNoTracking().Where(r => r.Abbreviation == abbreviation.Trim());
        if (excludeId is not null) q = q.Where(r => r.Id != excludeId.Value);
        return await q.AnyAsync(cancellationToken);
    }
}
