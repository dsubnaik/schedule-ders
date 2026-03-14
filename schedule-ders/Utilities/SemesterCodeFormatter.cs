namespace schedule_ders.Utilities;

public static class SemesterCodeFormatter
{
    public static string ToDisplayName(string? semesterCode)
    {
        var normalized = (semesterCode ?? string.Empty).Trim();
        if (normalized.Length != 6 || !int.TryParse(normalized[..4], out var year))
        {
            return string.IsNullOrWhiteSpace(normalized) ? "-" : normalized;
        }

        var termCode = normalized[4..];
        var termName = termCode switch
        {
            "01" => "Spring",
            "05" => "Summer",
            "09" => "Fall",
            _ => $"Term {termCode}"
        };

        return $"{termName} {year}";
    }

    public static bool IsPastSemester(string? semesterCode, DateTime todayLocal)
    {
        var normalized = (semesterCode ?? string.Empty).Trim();
        if (normalized.Length != 6 || !int.TryParse(normalized[..4], out var year))
        {
            return false;
        }

        if (!int.TryParse(normalized[4..], out var term))
        {
            return false;
        }

        var cutoff = term switch
        {
            1 => new DateTime(year, 6, 1),
            5 => new DateTime(year, 9, 1),
            9 => new DateTime(year, 12, 20),
            _ => new DateTime(year, 12, 31)
        };

        return todayLocal.Date >= cutoff.Date;
    }
}
