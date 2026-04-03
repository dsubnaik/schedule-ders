namespace schedule_ders.Utilities;

public sealed record LeaderCandidateEntry(string CandidateName, string CandidateANumber)
{
    public string DisplayName => string.IsNullOrWhiteSpace(CandidateANumber)
        ? CandidateName
        : $"{CandidateName} ({CandidateANumber})";

    public string EncodedValue => string.IsNullOrWhiteSpace(CandidateANumber)
        ? CandidateName
        : $"{CandidateName}|{CandidateANumber}";
}

public static class LeaderCandidateCodec
{
    public static List<LeaderCandidateEntry> Parse(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return [];
        }

        var lines = rawValue
            .Replace("\r", "\n")
            .Replace(";", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var results = new List<LeaderCandidateEntry>();
        foreach (var line in lines)
        {
            var separatorIndex = line.IndexOf('|');
            var candidateName = separatorIndex >= 0
                ? line[..separatorIndex].Trim()
                : line.Trim();
            var candidateANumber = separatorIndex >= 0
                ? line[(separatorIndex + 1)..].Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(candidateName))
            {
                continue;
            }

            var exists = results.Any(entry =>
                string.Equals(entry.CandidateName, candidateName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.CandidateANumber, candidateANumber, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                results.Add(new LeaderCandidateEntry(candidateName, candidateANumber));
            }
        }

        return results;
    }

    public static string Normalize(string? rawValue)
    {
        var entries = Parse(rawValue);
        return Serialize(entries);
    }

    public static string Serialize(IEnumerable<LeaderCandidateEntry> entries)
    {
        var uniqueEntries = new List<LeaderCandidateEntry>();
        foreach (var entry in entries)
        {
            var candidateName = (entry.CandidateName ?? string.Empty).Trim();
            var candidateANumber = (entry.CandidateANumber ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(candidateName))
            {
                continue;
            }

            var exists = uniqueEntries.Any(existing =>
                string.Equals(existing.CandidateName, candidateName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.CandidateANumber, candidateANumber, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                uniqueEntries.Add(new LeaderCandidateEntry(candidateName, candidateANumber));
            }
        }

        return string.Join('\n', uniqueEntries.Select(entry => entry.EncodedValue));
    }
}
