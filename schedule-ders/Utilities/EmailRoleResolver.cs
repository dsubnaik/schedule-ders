namespace schedule_ders.Utilities;

public static class EmailRoleResolver
{
    public const string ProfessorDomain = "tamucc.edu";
    public const string StudentDomain = "islander.tamucc.edu";

    public static string? ResolveRole(string? email)
    {
        var domain = GetDomain(email);

        return domain switch
        {
            ProfessorDomain => "Professor",
            StudentDomain => "Student",
            _ => null
        };
    }

    public static string GetSupportedDomainsMessage()
    {
        return $"Use a @{ProfessorDomain} professor email or @{StudentDomain} student email.";
    }

    private static string? GetDomain(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
        {
            return null;
        }

        return email[(atIndex + 1)..].Trim().ToLowerInvariant();
    }
}
