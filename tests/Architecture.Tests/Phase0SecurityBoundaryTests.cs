using Workforce.BuildingBlocks.Database;
using Xunit;

namespace Architecture.Tests;

public sealed class Phase0SecurityBoundaryTests
{
    [Fact]
    public void DatabaseConnection_UsesExplicitConfiguredString()
    {
        var resolved = DatabaseConnectionResolver.Resolve(
            "Host=configured;Database=workforce;Username=app;Password=external",
            _ => throw new InvalidOperationException("Environment must not be consulted when configuration exists."));

        Assert.Equal("Host=configured;Database=workforce;Username=app;Password=external", resolved);
    }

    [Fact]
    public void DatabaseConnection_WithoutConfiguredSecret_FailsClosed()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DatabaseConnectionResolver.Resolve(null, _ => null));

        Assert.True(exception.Message.Contains("ZAINX_DB_PASSWORD", StringComparison.Ordinal));
    }

    [Fact]
    public void DatabaseConnection_CanUseExplicitEnvironmentForLocalDevelopment()
    {
        var values = new Dictionary<string, string?>
        {
            ["ZAINX_DB_HOST"] = "localhost",
            ["ZAINX_DB_PORT"] = "55432",
            ["ZAINX_DB_NAME"] = "zainx_workforce",
            ["ZAINX_DB_USER"] = "zainx",
            ["ZAINX_DB_PASSWORD"] = "local-only-secret"
        };

        var resolved = DatabaseConnectionResolver.Resolve(null, values.GetValueOrDefault);

        Assert.True(resolved.Contains("Host=localhost", StringComparison.Ordinal));
        Assert.True(resolved.Contains("Port=55432", StringComparison.Ordinal));
        Assert.False(resolved.Contains("123456", StringComparison.Ordinal));
    }

    [Fact]
    public void MigrationReadiness_IsNotReadyUntilStartupCompletes()
    {
        var state = new MigrationReadinessState();

        Assert.False(state.IsReady);

        state.MarkReady();
        Assert.True(state.IsReady);
        Assert.Null(state.FailureReason);

        state.MarkFailed(new InvalidOperationException("migration failed"));
        Assert.False(state.IsReady);
        Assert.Equal("migration failed", state.FailureReason);
    }
}
