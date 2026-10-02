using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExpenseGuard.Worker;

public sealed class WorkerHeartbeat(TimeProvider clock) : IHealthCheck
{
    private long lastTick;
    private int started;
    public void Tick()
    {
        Interlocked.Exchange(ref lastTick, clock.GetTimestamp());
        Volatile.Write(ref started, 1);
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var recent = Volatile.Read(ref started) == 1
            && clock.GetElapsedTime(Interlocked.Read(ref lastTick)) < TimeSpan.FromSeconds(15);
        return Task.FromResult(recent ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Worker loop has not reported a recent heartbeat."));
    }
}

public sealed class WorkerHeartbeatService(WorkerHeartbeat heartbeat, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5), clock);
        heartbeat.Tick();
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            heartbeat.Tick();
        }
    }
}
