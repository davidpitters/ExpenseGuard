using ExpenseGuard.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExpenseGuard.UnitTests;

public sealed class HeartbeatTests
{
    [Fact]
    public async Task Stopped_or_stalled_loop_is_not_ready()
    {
        var clock = new TestClock();
        var heartbeat = new WorkerHeartbeat(clock);
        Assert.Equal(HealthStatus.Unhealthy, (await heartbeat.CheckHealthAsync(new())).Status);
        heartbeat.Tick();
        Assert.Equal(HealthStatus.Healthy, (await heartbeat.CheckHealthAsync(new())).Status);
        clock.Timestamp += 16;
        Assert.Equal(HealthStatus.Unhealthy, (await heartbeat.CheckHealthAsync(new())).Status);
    }

    private sealed class TestClock : TimeProvider
    {
        public long Timestamp { get; set; }
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => Timestamp;
    }
}
