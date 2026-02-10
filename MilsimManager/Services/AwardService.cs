using Microsoft.EntityFrameworkCore;
using MilsimManager.Models;

namespace MilsimManager.Services;

public class AwardService(IDbContextFactory<Context> dbFactory) : IAwardService {
    public async Task<List<Award>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Awards.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search)) {
            search = search.Trim().ToLower();
            q = q.Where(a =>
                a.Name.ToLower().Contains(search) ||
                (a.Description != null && a.Description.ToLower().Contains(search)));
        }

        return await q
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Award?> GetByIdAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Awards
            .AsNoTracking()
            .Include(a => a.UserAwards)
            .ThenInclude(ua => ua.User)
            .ThenInclude(u => u.Rank)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Award> CreateAsync(string name, string? description, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var award = new Award {
            Name = string.IsNullOrEmpty(name) ? throw new AppException("Name is required") : name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

        db.Awards.Add(award);

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return award;
    }

    public async Task<Award> UpdateAsync(int id, uint version, string name, string? description, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        Award award;
        try {
            award = await db.Awards.SingleAsync(a => a.Id == id, cancellationToken);
        } catch (InvalidOperationException) {
            throw new AppException("Award not found");
        }
        db.Entry(award).Property(a => a.Version).OriginalValue = version;

        award.Name = string.IsNullOrEmpty(name) ? throw new AppException("Name is required") : name.Trim();
        award.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return award;
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var deleted = await db.Awards.Where(a => a.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? throw new AppException("Award not found") : deleted;
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Awards.AsNoTracking().Where(a => a.Name == name);
        if (excludeId is not null) q = q.Where(a => a.Id != excludeId.Value);
        return await q.AnyAsync(cancellationToken);
    }

    public async Task AssignUserAsync(
        int awardId,
        int userId,
        string? comment,
        User approvedBy,
        CancellationToken cancellationToken = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var awardExists = await db.Awards
            .AsNoTracking()
            .AnyAsync(a => a.Id == awardId, cancellationToken);
        if (!awardExists) throw new AppException("Award not found");

        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (comment is not null && comment.Length > 1000)
            throw new AppException("Comment must be at most 1000 characters");

        var userExists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists) throw new AppException("User not found");

        var exists = await db.UserAwards
            .AsNoTracking()
            .AnyAsync(ua => ua.AwardId == awardId && ua.UserId == userId, cancellationToken);
        if (exists) throw new AppException("User already has this award");

        db.UserAwards.Add(new UserAward {
            UserId = userId,
            AwardId = awardId,
            Date = DateTime.UtcNow,
            Comment = comment,
            ApprovedById = approvedBy.Id,
            ApprovedByName = approvedBy.Name
        });
        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }
    }

    public async Task RemoveUserAsync(
        int awardId,
        int userId,
        CancellationToken cancellationToken = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var award = await db.Awards
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == awardId, cancellationToken);
        if (award is null) throw new AppException("Award not found");

        var link = await db.UserAwards
            .SingleOrDefaultAsync(ua => ua.AwardId == awardId && ua.UserId == userId, cancellationToken);
        if (link is null) throw new AppException("Award assignment not found");

        db.UserAwards.Remove(link);
        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }
    }
}
