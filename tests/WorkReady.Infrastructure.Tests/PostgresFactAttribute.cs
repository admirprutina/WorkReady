namespace WorkReady.Infrastructure.Tests;

/// <summary>
/// A test that needs a real PostgreSQL database. Runs only when <see cref="ConnectionStringVariable"/> is set,
/// e.g. to the docker-compose database; otherwise it is reported as skipped, never as passed.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string ConnectionStringVariable = "WORKREADY_TEST_POSTGRES";

    public PostgresFactAttribute()
    {
        if (ConnectionString is null)
        {
            Skip = $"Set {ConnectionStringVariable} to a PostgreSQL connection string to run this test.";
        }
    }

    public static string? ConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionStringVariable) is { Length: > 0 } value ? value : null;
}
