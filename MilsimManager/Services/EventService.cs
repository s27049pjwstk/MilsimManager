using Microsoft.EntityFrameworkCore;
using MilsimManager.Models;

namespace MilsimManager.Services;

public class EventService(IDbContextFactory<Context> dbFactory) : IEventService {
    public async Task<List<Event>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search)) {
            search = search.Trim().ToLower();
            q = q.Where(e =>
                e.Name.ToLower().Contains(search) ||
                (e.Description != null && e.Description.ToLower().Contains(search)));
        }

        return await q
            .OrderByDescending(e => e.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Event>> GetUpcomingAsync(CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;
        return await db.Events
            .AsNoTracking()
            .Where(e => e.Date >= now && e.Date <= now.AddDays(30))
            .OrderBy(e => e.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Events
            .AsNoTracking()
            .Include(e => e.UserAttendances)
            .ThenInclude(ua => ua.User)
            .ThenInclude(u => u.Rank)
            .Include(e => e.UserAttendances)
            .ThenInclude(ua => ua.User)
            .ThenInclude(u => u.Unit)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Event> CreateAsync(string name, string? description, DateTime date, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (date.ToUniversalTime() < DateTime.UtcNow)
            throw new AppException("Event date cannot be in the past");

        var ev = new Event {
            Name = string.IsNullOrEmpty(name) ? throw new AppException("Name is required") : name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Date = date.ToUniversalTime()
        };

        if (ev.Description is not null && ev.Description.Length > 2000)
            throw new AppException("Description must be at most 2000 characters");

        db.Events.Add(ev);

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return ev;
    }

    public async Task<Event> UpdateAsync(int id, uint version, string name, string? description, DateTime date, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (date.ToUniversalTime() < DateTime.UtcNow)
            throw new AppException("Event date cannot be in the past");

        Event ev;
        try {
            ev = await db.Events.SingleAsync(e => e.Id == id, cancellationToken);
        } catch (InvalidOperationException) {
            throw new AppException("Event not found");
        }
        db.Entry(ev).Property(e => e.Version).OriginalValue = version;

        ev.Name = string.IsNullOrEmpty(name) ? throw new AppException("Name is required") : name.Trim();
        ev.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ev.Date = date.ToUniversalTime();

        if (ev.Description is not null && ev.Description.Length > 2000)
            throw new AppException("Description must be at most 2000 characters");

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            throw new AppException("Concurrency error");
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }

        return ev;
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var deleted = await db.Events.Where(e => e.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? throw new AppException("Event not found") : deleted;
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var q = db.Events.AsNoTracking().Where(e => e.Name == name);
        if (excludeId is not null) q = q.Where(e => e.Id != excludeId.Value);
        return await q.AnyAsync(cancellationToken);
    }

    public async Task AttendAsync(int eventId, int userId, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var eventDate = await db.Events
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .Select(e => (DateTime?)e.Date)
            .SingleOrDefaultAsync(cancellationToken);
        if (eventDate is null) throw new AppException("Event not found");
        if (eventDate.Value <= DateTime.UtcNow) throw new AppException("Cannot change attendance for past events");

        var userExists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists) throw new AppException("User not found");

        var exists = await db.UserAttendances
            .AsNoTracking()
            .AnyAsync(ua => ua.EventId == eventId && ua.UserId == userId, cancellationToken);
        if (exists) throw new AppException("Already attending");

        db.UserAttendances.Add(new UserAttendance {
            UserId = userId,
            EventId = eventId
        });

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }
    }

    public async Task UnattendAsync(int eventId, int userId, CancellationToken cancellationToken = default) {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var eventDate = await db.Events
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .Select(e => (DateTime?)e.Date)
            .SingleOrDefaultAsync(cancellationToken);
        if (eventDate is null) throw new AppException("Event not found");
        if (eventDate.Value <= DateTime.UtcNow) throw new AppException("Cannot change attendance for past events");

        var link = await db.UserAttendances
            .SingleOrDefaultAsync(ua => ua.EventId == eventId && ua.UserId == userId, cancellationToken);
        if (link is null) throw new AppException("Not attending");

        db.UserAttendances.Remove(link);
        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            throw new AppException("Failed to update database");
        }
    }
}
