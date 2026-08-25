using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Workforce.BuildingBlocks.Database;

namespace Workforce.Host.Api.Health;

public sealed class MigrationReadinessHealthCheck : IHealthCheck
{
    private readonly MigrationReadinessState _state;

    public MigrationReadinessHealthCheck(MigrationReadinessState state)
    {
        _state = state;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = _state.IsReady
            ? HealthCheckResult.Healthy("Database migrations completed.")
            : HealthCheckResult.Unhealthy(
                _state.FailureReason ?? "Database migrations have not completed.");

        return Task.FromResult(result);
    }
}

public sealed class DatabaseConnectivityHealthCheck : IHealthCheck
{
    private readonly NpgsqlDataSource _dataSource;

    public DatabaseConnectivityHealthCheck(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database connection is available.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database connection is unavailable.", exception);
        }
    }
}
