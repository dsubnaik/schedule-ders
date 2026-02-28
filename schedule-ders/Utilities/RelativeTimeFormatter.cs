namespace schedule_ders.Utilities;

public static class RelativeTimeFormatter
{
    public static string Format(DateTime? utc, DateTime? nowUtc = null)
    {
        if (!utc.HasValue)
        {
            return "No updates yet";
        }

        var now = nowUtc ?? DateTime.UtcNow;
        var delta = now - utc.Value;
        if (delta.TotalMinutes < 1)
        {
            return "just now";
        }

        if (delta.TotalHours < 1)
        {
            var minutes = Math.Max(1, (int)delta.TotalMinutes);
            return $"{minutes} minute{(minutes == 1 ? string.Empty : "s")} ago";
        }

        if (delta.TotalDays < 1)
        {
            var hours = Math.Max(1, (int)delta.TotalHours);
            return $"{hours} hour{(hours == 1 ? string.Empty : "s")} ago";
        }

        if (delta.TotalDays < 7)
        {
            var days = Math.Max(1, (int)delta.TotalDays);
            return $"{days} day{(days == 1 ? string.Empty : "s")} ago";
        }

        if (delta.TotalDays < 31)
        {
            var weeks = Math.Max(1, (int)(delta.TotalDays / 7));
            return $"{weeks} week{(weeks == 1 ? string.Empty : "s")} ago";
        }

        if (delta.TotalDays < 365)
        {
            var months = Math.Max(1, (int)(delta.TotalDays / 30));
            return $"{months} month{(months == 1 ? string.Empty : "s")} ago";
        }

        var years = Math.Max(1, (int)(delta.TotalDays / 365));
        return $"{years} year{(years == 1 ? string.Empty : "s")} ago";
    }
}
