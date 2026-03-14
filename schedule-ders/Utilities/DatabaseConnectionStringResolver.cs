using Npgsql;

namespace schedule_ders.Utilities;

public static class DatabaseConnectionStringResolver
{
    public static string ResolveConfigurationConnectionString(IConfiguration configuration)
    {
        var configuredConnection = configuration.GetConnectionString("DefaultConnection");
        var databaseUrl = configuration["DATABASE_URL"];

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return BuildPostgresConnectionString(databaseUrl);
        }

        if (!string.IsNullOrWhiteSpace(configuredConnection))
        {
            return configuredConnection;
        }

        throw new InvalidOperationException("DefaultConnection or DATABASE_URL must be configured.");
    }

    private static string BuildPostgresConnectionString(string databaseUrl)
    {
        if (databaseUrl.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            return databaseUrl;
        }

        var databaseUri = new Uri(databaseUrl);
        var userInfoParts = databaseUri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.Port,
            Database = databaseUri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfoParts[0]),
            Password = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : string.Empty,
            SslMode = SslMode.Require
        };

        if (!string.IsNullOrWhiteSpace(databaseUri.Query))
        {
            var queryPairs = databaseUri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var pair in queryPairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                builder[key] = value;
            }
        }

        return builder.ConnectionString;
    }
}
