using Microsoft.Extensions.Hosting;

namespace SdmCo.Reddit.Core.Monitors;

public class SubredditMonitorHostedService : IHostedService
{
    private readonly SubredditMonitor _subredditMonitor;

    public SubredditMonitorHostedService(SubredditMonitor subredditMonitor) => _subredditMonitor = subredditMonitor;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => _subredditMonitor.MonitorAsync(cancellationToken), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}