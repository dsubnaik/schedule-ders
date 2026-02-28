using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;

namespace schedule_ders.Data;

public static class SessionDeduper
{
    public static async Task<int> DeduplicateAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScheduleContext>();

        var allSessions = await context.Sessions
            .AsNoTracking()
            .Select(s => new
            {
                s.SessionID,
                s.CourseID,
                s.Day,
                s.Time,
                s.Location
            })
            .OrderBy(s => s.SessionID)
            .ToListAsync();

        var duplicateIds = allSessions
            .GroupBy(s => new { s.CourseID, s.Day, s.Time, s.Location })
            .SelectMany(g => g.Skip(1))
            .Select(s => s.SessionID)
            .ToList();

        if (duplicateIds.Count == 0)
        {
            return 0;
        }

        var duplicates = await context.Sessions
            .Where(s => duplicateIds.Contains(s.SessionID))
            .ToListAsync();

        context.Sessions.RemoveRange(duplicates);
        await context.SaveChangesAsync();

        return duplicates.Count;
    }
}
