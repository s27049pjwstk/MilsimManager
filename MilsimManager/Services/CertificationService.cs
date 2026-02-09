using Microsoft.EntityFrameworkCore;
using MilsimManager.Models;

namespace MilsimManager.Services;

public class CertificationService(IDbContextFactory<Context> dbFactory) : ICertificationService {
    public async Task<List<Certification>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Certifications.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search)) {
            search = search.Trim().ToLower();
            q = q.Where(c =>
                c.Name.ToLower().Contains(search) ||
                (c.Description != null && c.Description.ToLower().Contains(search)));
        }

        return await q
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Certification?> GetByIdAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Certifications
            .AsNoTracking()
            .Include(c => c.UserCertifications)
            .ThenInclude(uc => uc.User)
            .ThenInclude(u => u.Rank)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Certification> CreateAsync(string name, string? description, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var certification = new Certification {
            Name = string.IsNullOrEmpty(name) ? throw new AppException("Name is required") : name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

        db.Certifications.Add(certification);

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return certification;
    }

    public async Task<Certification> UpdateAsync(int id, uint version, string name, string? description, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        Certification certification;
        try {
            certification = await db.Certifications.SingleAsync(c => c.Id == id, cancellationToken);
        } catch (InvalidOperationException) {
            throw new AppException("Certification not found");
        }
        db.Entry(certification).Property(c => c.Version).OriginalValue = version;

        certification.Name = string.IsNullOrEmpty(name) ? throw new AppException("Name is required") : name.Trim();
        certification.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return certification;
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var deleted = await db.Certifications.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? throw new AppException("Certification not found") : deleted;
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Certifications.AsNoTracking().Where(c => c.Name == name);
        if (excludeId is not null) q = q.Where(c => c.Id != excludeId.Value);
        return await q.AnyAsync(cancellationToken);
    }

    public async Task AssignUserAsync(
        int certificationId,
        int userId,
        string? comment,
        User approvedBy,
        CancellationToken cancellationToken = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var certificationExists = await db.Certifications
            .AsNoTracking()
            .AnyAsync(c => c.Id == certificationId, cancellationToken);
        if (!certificationExists) throw new AppException("Certification not found");

        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (comment is not null && comment.Length > 1000)
            throw new AppException("Comment must be at most 1000 characters");

        var userExists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists) throw new AppException("User not found");

        var exists = await db.UserCertifications
            .AsNoTracking()
            .AnyAsync(uc => uc.CertificationId == certificationId && uc.UserId == userId, cancellationToken);
        if (exists) throw new AppException("User already has this certification");

        db.UserCertifications.Add(new UserCertification {
            UserId = userId,
            CertificationId = certificationId,
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
        int certificationId,
        int userId,
        CancellationToken cancellationToken = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var certification = await db.Certifications
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == certificationId, cancellationToken);
        if (certification is null) throw new AppException("Certification not found");

        var link = await db.UserCertifications
            .SingleOrDefaultAsync(uc => uc.CertificationId == certificationId && uc.UserId == userId, cancellationToken);
        if (link is null) throw new AppException("Certification assignment not found");

        db.UserCertifications.Remove(link);
        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }
    }
}
