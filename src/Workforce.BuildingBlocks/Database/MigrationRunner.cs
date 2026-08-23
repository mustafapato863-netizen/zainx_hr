namespace Workforce.BuildingBlocks.Database;

public static class MigrationRunner
{
    public static async Task<bool> EnsureMigrationHistoryTableAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new Npgsql.NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            CREATE SCHEMA IF NOT EXISTS platform;
            CREATE TABLE IF NOT EXISTS platform.__migration_history (
                migration_id VARCHAR(150) PRIMARY KEY,
                module_name VARCHAR(100) NOT NULL,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ";

        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
        return true;
    }
}
