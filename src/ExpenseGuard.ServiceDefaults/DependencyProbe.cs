using Microsoft.Extensions.Configuration;
using Npgsql;
using StackExchange.Redis;

namespace ExpenseGuard.ServiceDefaults;

public sealed record ProbeResult(string Status, string Detail)
{
    public static ProbeResult Healthy { get; } = new("healthy", "Connection verified.");
    public static ProbeResult Missing { get; } = new("not_configured", "Local connection is not configured.");
    public static ProbeResult Unavailable { get; } = new("unavailable", "Dependency did not pass its readiness check.");
}

public interface IDependencyProbe
{
    Task<ProbeResult> CheckAsync(string dependency, CancellationToken cancellationToken);
}

public sealed class DependencyProbe(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IDependencyProbe
{
    public async Task<ProbeResult> CheckAsync(string dependency, CancellationToken cancellationToken)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            return dependency switch
            {
                "postgres" => await CheckPostgresAsync(budget.Token),
                "redis" => await CheckRedisAsync(budget.Token),
                "object-storage" => await CheckHttpAsync(configuration["ObjectStorage:Endpoint"], "minio/health/ready", budget.Token),
                "worker" => await CheckHttpAsync(configuration["Services:Worker"], "health/ready", budget.Token),
                "mcp" => await CheckHttpAsync(configuration["Services:Mcp"], "health/ready", budget.Token),
                _ => throw new ArgumentOutOfRangeException(nameof(dependency))
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProbeResult.Unavailable;
        }
        catch (Exception exception) when (exception is NpgsqlException or RedisException
            or HttpRequestException or TimeoutException or ArgumentException)
        {
            // Health deliberately exposes no exception, connection string, credential, or document content.
            return ProbeResult.Unavailable;
        }
    }

    private async Task<ProbeResult> CheckPostgresAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return ProbeResult.Missing;
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'vector')", connection);
        command.CommandTimeout = 2;
        return await command.ExecuteScalarAsync(cancellationToken) is true
            ? ProbeResult.Healthy : ProbeResult.Unavailable;
    }

    private async Task<ProbeResult> CheckRedisAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return ProbeResult.Missing;
        }

        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = true;
        options.ConnectRetry = 0;
        options.ConnectTimeout = 1000;
        options.AsyncTimeout = 1000;
        using var connection = await ConnectionMultiplexer.ConnectAsync(options);
        await connection.GetDatabase().PingAsync().WaitAsync(cancellationToken);
        return ProbeResult.Healthy;
    }

    private async Task<ProbeResult> CheckHttpAsync(string? endpoint, string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return ProbeResult.Missing;
        }

        var uri = new Uri(new Uri(endpoint.TrimEnd('/') + "/"), path);
        using var response = await httpClientFactory.CreateClient("health")
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return response.IsSuccessStatusCode ? ProbeResult.Healthy : ProbeResult.Unavailable;
    }
}
