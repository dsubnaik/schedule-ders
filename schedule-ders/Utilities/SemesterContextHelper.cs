namespace schedule_ders.Utilities;

public static class SemesterContextHelper
{
    public const string AdminSemesterCookieKey = "sd-admin-selected-semester";

    public static int? ReadSelectedSemesterId(HttpRequest request)
    {
        if (int.TryParse(request.Cookies[AdminSemesterCookieKey], out var semesterId))
        {
            return semesterId;
        }

        return null;
    }
}
