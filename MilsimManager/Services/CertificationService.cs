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
            .Include(c => c.UserCertifications).ThenInclude(uc => uc.User)
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
}
