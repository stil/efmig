namespace Efmig.Core;

public static class DbContextCodeGenerator
{
    public static string GenerateCode(
        string databaseProvider,
        bool useConnectionStringDirectly,
        string connectionString,
        string server,
        string port,
        string database,
        string username,
        string password)
    {
        var connStr = useConnectionStringDirectly
            ? connectionString
            : BuildConnectionString(databaseProvider, server, port, database, username, password);

        if (string.IsNullOrWhiteSpace(connStr))
        {
            return "// Configure connection string first";
        }

        return databaseProvider switch
        {
            "PostgreSQL" => $"""optionsBuilder.UseNpgsql("{connStr}");""",
            "MySQL" =>
                $"""
                 var connectionString = "{connStr}";
                 var serverVersion = ServerVersion.AutoDetect(connectionString);
                 optionsBuilder.UseMySql(connectionString, serverVersion);
                 """,
            "SqlServer" => $"""optionsBuilder.UseSqlServer("{connStr}");""",
            "SQLite" => $"""optionsBuilder.UseSqlite("{connStr}");""",
            _ => string.Empty
        };
    }

    public static string BuildConnectionString(
        string databaseProvider,
        string server,
        string port,
        string database,
        string username,
        string password)
    {
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            return string.Empty;
        }

        var effectivePort = port;
        if (string.IsNullOrWhiteSpace(effectivePort))
        {
            effectivePort = GetDefaultPort(databaseProvider);
        }

        return databaseProvider switch
        {
            "PostgreSQL" => $"""Host={server};Port={effectivePort};Database={database};Username={username};Password={password}""",
            "MySQL" => $"""server={server};Port={effectivePort};user={username};password={password};database={database}""",
            "SqlServer" => $"""Server={server},{effectivePort};Database={database};User Id={username};Password={password}""",
            "SQLite" => $"""Data Source={database}""",
            _ => string.Empty
        };
    }

    public static string GetDefaultPort(string databaseProvider)
    {
        return databaseProvider switch
        {
            "PostgreSQL" => "5432",
            "MySQL" => "3306",
            "SqlServer" => "1433",
            _ => ""
        };
    }
}