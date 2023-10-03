﻿using Microsoft.Extensions.Options;
using SdmCo.Reddit.Api.Settings;

namespace SdmCo.Reddit.Api.Monitors;

public class SubredditMonitorHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<Task> _monitoringTasks;
    private readonly SubredditSettings _subredditSettings;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<SubredditMonitorHostedService> _logger;

    public SubredditMonitorHostedService(IServiceProvider serviceProvider, IOptions<SubredditSettings> subredditSettings, ILogger<SubredditMonitorHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _subredditSettings = subredditSettings.Value;
        _monitoringTasks = new List<Task>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subreddit Monitor Hosted Service started.");

        var subreddits = _subredditSettings.Subreddits;

        _logger.LogInformation("Creating monitors for the following subreddits: {SubredditNames}", string.Join(',', subreddits));

        foreach (var subreddit in subreddits)
        {
            // Spwan a new task to monitor each subreddit in our list
            var task = Task.Run(async () =>
            {
                // Create and configure our subreddit monitor
                using var scope = _serviceProvider.CreateScope();
                var subredditMonitor = scope.ServiceProvider.GetRequiredService<SubredditMonitor>();
                subredditMonitor.ConfigureSubreddit(subreddit);
                await subredditMonitor.MonitorAsync(_cancellationTokenSource.Token);
            }, cancellationToken);

            // Keep track of our monitoring tasks
            _monitoringTasks.Add(task);
        }

        // Wait for them all to complete
        return Task.WhenAll(_monitoringTasks);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping all subreddit monitors.");

        // Cancel all our monitoring tasks
        _cancellationTokenSource.Cancel();

        return Task.CompletedTask;
    }
}