using Npgsql;

namespace Workforce.BuildingBlocks.Database;

public static class DatabaseConnectionResolver
{
    public static string Resolve(
        string? configuredConnectionString,
        Func<string, string?>? environment = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString.Trim();
        }

        environment ??= Environment.GetEnvironmentVariable;

        var password = environment("ZAINX_DB_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Database connection is not configured. Set ConnectionStrings:DefaultConnection " +
                "or provide ZAINX_DB_PASSWORD through the deployment environment.");
        }

        var portValue = environment("ZAINX_DB_PORT") ?? "55432";
        if (!int.TryParse(portValue, out var port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException("ZAINX_DB_PORT must be a valid TCP port.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = environment("ZAINX_DB_HOST") ?? "127.0.0.1",
            Port = port,
            Database = environment("ZAINX_DB_NAME") ?? "zainx_workforce",
            Username = environment("ZAINX_DB_USER") ?? "zainx",
            Password = password
        };

        return builder.ConnectionString;
    }
}
