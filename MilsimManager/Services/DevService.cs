using Microsoft.EntityFrameworkCore;
using MilsimManager.Models;

namespace MilsimManager.Services;

public class DevService(IDbContextFactory<Context> dbFactory) : IDevService {
    public async Task ResetAsync() {
        await using var db = await dbFactory.CreateDbContextAsync();

        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        await SeedExampleDataAsync(db);
    }

    private static async Task SeedExampleDataAsync(Context db) {
        if (await db.Users.AnyAsync()) return;

        var today = DateTime.UtcNow.Date;

        var unitCmd = new Unit { Name = "Command", Abbreviation = "HQ", Description = "Command and admin." };
        var unitAlpha = new Unit { Name = "Alpha", Abbreviation = "A", Description = "Infantry squad." };
        var unitBravo = new Unit { Name = "Bravo", Abbreviation = "B", Description = "Infantry squad." };
        db.Units.AddRange(unitCmd, unitAlpha, unitBravo);

        var rankPvt = new Rank { Name = "Private", Code = "E-1", Abbreviation = "PVT", SortOrder = 1 };
        var rankCpl = new Rank { Name = "Corporal", Code = "E-2", Abbreviation = "CPL", SortOrder = 2 };
        var rankSgt = new Rank { Name = "Sergeant", Code = "E-3", Abbreviation = "SGT", SortOrder = 3 };
        var rankSsg = new Rank { Name = "Staff Sergeant", Code = "E-4", Abbreviation = "SSG", SortOrder = 4 };
        db.Ranks.AddRange(rankPvt, rankCpl, rankSgt, rankSsg);

        var certMedic = new Certification { Name = "Combat Medic" };
        var certRto = new Certification { Name = "RTO" };
        var certAt = new Certification { Name = "AT Specialist" };
        db.Certifications.AddRange(certMedic, certRto, certAt);

        var awardVeteran = new Award { Name = "1 year veteran", Description = "Example award." };
        var awardSharpshooter = new Award { Name = "Sharpshooter", Description = "Example award." };
        var awardServiceStar = new Award { Name = "Service Star", Description = "Example award." };
        db.Awards.AddRange(awardVeteran, awardSharpshooter, awardServiceStar);

        var eventPast1 = new Event { Name = "Operation Ember", Description = "Example past op.", Date = today.AddDays(-35) };
        var eventPast2 = new Event { Name = "Operation Dust", Description = "Example past op.", Date = today.AddDays(-14) };
        var eventFuture1 = new Event { Name = "Operation Frostbite", Description = "Night raid training.", Date = today.AddDays(7) };
        var eventFuture2 = new Event { Name = "Medical School Day 3", Description = null, Date = today.AddDays(14) };
        db.Events.AddRange(eventPast1, eventPast2, eventFuture1, eventFuture2);

        await db.SaveChangesAsync();

        var u1 = CreateUser("J. Doe",  today.AddDays(-400), rankSsg, unitCmd, "Officer", "1",true, true);
        var u2 = CreateUser("T. Able",  today.AddDays(-250), rankSgt, unitAlpha, "Squad Leader", "2");
        var u3 = CreateUser("M. Marco",  today.AddDays(-120), rankPvt, unitBravo, "Rifleman", "3");
        var u4 = CreateUser("S. Nova",  today.AddDays(-60), rankPvt, unitAlpha, "Medic", "4");
        var u5 = CreateUser("K. Voss",  today.AddDays(-3), null, null, null, "5",false);
        db.Users.AddRange(u1, u2, u3, u4, u5);

        await db.SaveChangesAsync();

        db.RankLogs.AddRange(
        new RankLog { User = u1, Rank = rankCpl, RankName = rankCpl.Name, Date = today.AddDays(-200) },
        new RankLog { User = u1, Rank = rankSgt, RankName = rankSgt.Name, Date = today.AddDays(-90) },
        new RankLog { User = u2, Rank = rankPvt, RankName = rankPvt.Name, Date = today.AddDays(-220) },
        new RankLog { User = u2, Rank = rankCpl, RankName = rankCpl.Name, Date = today.AddDays(-120) },
        new RankLog { User = u3, Rank = rankPvt, RankName = rankPvt.Name, Date = today.AddDays(-120) },
        new RankLog { User = u4, Rank = rankPvt, RankName = rankPvt.Name, Date = today.AddDays(-60) }
        );

        var l1 = new UnitAssignmentLog { User = u2, Unit = unitAlpha, UnitName = unitAlpha.Name, Role = "Rifleman", Date = today.AddDays(-200) };
        var l2 = new UnitAssignmentLog { User = u2, Unit = unitAlpha, UnitName = unitAlpha.Name, Role = u2.UnitRole, Date = today.AddDays(-140) };
        var l3 = new UnitAssignmentLog { User = u3, Unit = unitBravo, UnitName = unitBravo.Name, Role = u3.UnitRole, Date = today.AddDays(-120) };
        db.UnitAssignmentLogs.AddRange(l1, l2, l3);

        db.UserAwards.AddRange(
        new UserAward { User = u1, Award = awardVeteran, Date = today.AddDays(-30) },
        new UserAward { User = u1, Award = awardServiceStar, Date = today.AddDays(-10) },
        new UserAward { User = u2, Award = awardSharpshooter, Date = today.AddDays(-60) }
        );

        db.UserCertifications.AddRange(
        new UserCertification { User = u1, Certification = certRto, Date = today.AddDays(-100) },
        new UserCertification { User = u2, Certification = certAt, Date = today.AddDays(-80) },
        new UserCertification { User = u4, Certification = certMedic, Date = today.AddDays(-20) }
        );

        db.UserAttendances.AddRange(
        new UserAttendance { User = u1, Event = eventPast1 },
        new UserAttendance { User = u2, Event = eventPast1 },
        new UserAttendance { User = u3, Event = eventPast2 },
        new UserAttendance { User = u4, Event = eventPast2 },
        new UserAttendance { User = u1, Event = eventFuture1 },
        new UserAttendance { User = u2, Event = eventFuture1 }
        );

        await db.SaveChangesAsync();
    }

    private static User CreateUser(
        string name,
        DateTime dateJoined,
        Rank? rank,
        Unit? unit,
        string? role,
        string discordId,
        bool active = true,
        bool admin = false
    ) {
        return new User {
            Name = name,
            Active = active,
            Admin = admin,
            DateJoined = dateJoined,
            Rank = rank,
            Unit = unit,
            UnitRole = role,
            DiscordId = discordId
        };
    }
}
