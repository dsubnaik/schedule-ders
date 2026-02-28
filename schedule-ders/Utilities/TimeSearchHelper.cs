using System.Text;
using System.Text.RegularExpressions;

namespace schedule_ders.Utilities;

public static class TimeSearchHelper
{
    private static readonly Regex DigitsOnly = new(@"\D", RegexOptions.Compiled);
    private static readonly Regex TimeTokenRegex = new(
        @"^\s*(?<hour>\d{1,2})(?::?(?<minute>\d{2}))?\s*(?<ampm>am|pm)?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string ToCompactToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }

        return sb.ToString();
    }

    public static bool TryParseSearchTime(string? rawSearch, out TimeSpan time)
    {
        var parsed = ParseSearchTime(rawSearch);
        if (parsed is null)
        {
            time = default;
            return false;
        }

        time = parsed.Value.baseTime;
        return true;
    }

    public static bool MatchesTimeRange(string? rangeText, string? rawSearch)
    {
        if (string.IsNullOrWhiteSpace(rangeText) || string.IsNullOrWhiteSpace(rawSearch))
        {
            return false;
        }

        var parsed = ParseSearchTime(rawSearch);
        if (parsed is null)
        {
            return false;
        }

        if (IsTimeInsideRange(rangeText, parsed.Value.baseTime))
        {
            return true;
        }

        foreach (var candidate in parsed.Value.alternateTimes)
        {
            if (IsTimeInsideRange(rangeText, candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static (TimeSpan baseTime, List<TimeSpan> alternateTimes)? ParseSearchTime(string? rawSearch)
    {
        if (string.IsNullOrWhiteSpace(rawSearch))
        {
            return null;
        }

        var input = rawSearch.Trim().ToLowerInvariant();
        var match = TimeTokenRegex.Match(input);
        if (!match.Success)
        {
            var digits = DigitsOnly.Replace(input, string.Empty);
            if (digits.Length is 3 or 4)
            {
                var padded = digits.Length == 3 ? $"0{digits}" : digits;
                var hour = int.Parse(padded[..2]);
                var minute = int.Parse(padded[2..]);
                if (hour is >= 0 and <= 23 && minute is >= 0 and <= 59)
                {
                    var baseTime = new TimeSpan(hour, minute, 0);
                    var alternates = new List<TimeSpan>();
                    if (hour is >= 1 and <= 11)
                    {
                        alternates.Add(baseTime.Add(TimeSpan.FromHours(12)));
                    }
                    else if (hour == 12)
                    {
                        alternates.Add(new TimeSpan(0, minute, 0));
                    }

                    return (baseTime, alternates);
                }
            }

            return null;
        }

        var hourPart = int.Parse(match.Groups["hour"].Value);
        var minuteGroup = match.Groups["minute"].Value;
        var minutePart = string.IsNullOrWhiteSpace(minuteGroup) ? 0 : int.Parse(minuteGroup);
        var ampm = match.Groups["ampm"].Value.ToLowerInvariant();
        var hasMeridiem = !string.IsNullOrWhiteSpace(ampm);

        if (hourPart > 23 || minutePart > 59)
        {
            return null;
        }

        if (ampm == "am" && hourPart == 12)
        {
            hourPart = 0;
        }
        else if (ampm == "pm" && hourPart < 12)
        {
            hourPart += 12;
        }

        var parsedTime = new TimeSpan(hourPart, minutePart, 0);
        var alternateTimes = new List<TimeSpan>();
        if (!hasMeridiem && hourPart is >= 1 and <= 11)
        {
            alternateTimes.Add(parsedTime.Add(TimeSpan.FromHours(12)));
        }
        else if (!hasMeridiem && hourPart == 12)
        {
            alternateTimes.Add(new TimeSpan(0, minutePart, 0));
        }

        return (parsedTime, alternateTimes);
    }

    public static bool MatchesTimeText(string? rangeText, string? rawSearch)
    {
        if (string.IsNullOrWhiteSpace(rangeText) || string.IsNullOrWhiteSpace(rawSearch))
        {
            return false;
        }

        var compactSearch = ToCompactToken(rawSearch);
        if (string.IsNullOrWhiteSpace(compactSearch))
        {
            return false;
        }

        return ToCompactToken(rangeText).Contains(compactSearch, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTimeInsideRange(string? rangeText, TimeSpan targetTime)
    {
        if (string.IsNullOrWhiteSpace(rangeText))
        {
            return false;
        }

        var parts = rangeText.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!TryParseBound(parts[0], null, out var start))
        {
            return false;
        }

        if (!TryParseBound(parts[1], start.ampmHint, out var end))
        {
            return false;
        }

        var startValue = start.time;
        var endValue = end.time;
        if (endValue < startValue)
        {
            endValue = endValue.Add(TimeSpan.FromHours(12));
            if (targetTime < startValue)
            {
                targetTime = targetTime.Add(TimeSpan.FromHours(12));
            }
        }

        return targetTime >= startValue && targetTime <= endValue;
    }

    private static bool TryParseBound(string rawValue, string? fallbackAmpm, out (TimeSpan time, string? ampmHint) parsed)
    {
        parsed = default;
        var input = rawValue.Trim().ToLowerInvariant();
        var match = TimeTokenRegex.Match(input);
        if (!match.Success)
        {
            return false;
        }

        var hour = int.Parse(match.Groups["hour"].Value);
        var minuteGroup = match.Groups["minute"].Value;
        var minute = string.IsNullOrWhiteSpace(minuteGroup) ? 0 : int.Parse(minuteGroup);
        var ampm = match.Groups["ampm"].Success ? match.Groups["ampm"].Value.ToLowerInvariant() : fallbackAmpm;

        if (hour > 23 || minute > 59)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(ampm))
        {
            if (hour == 12 && ampm == "am")
            {
                hour = 0;
            }
            else if (ampm == "pm" && hour < 12)
            {
                hour += 12;
            }
        }

        parsed = (new TimeSpan(hour, minute, 0), ampm);
        return true;
    }
}
