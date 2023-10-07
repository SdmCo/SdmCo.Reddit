using SdmCo.Reddit.Common.Persistence;
using SdmCo.Reddit.Monitor.Extensions;
using SdmCo.Reddit.Monitor.Monitors;
using SdmCo.Reddit.Monitor.Policies;
using SdmCo.Reddit.Monitor.Services;
using SdmCo.Reddit.Monitor.Settings;
using StackExchange.Redis;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        // Configure RedditAuthSettings and SubredditSettings
        services.Configure<RedditAuthSettings>(configuration.GetSection(RedditAuthSettings.SectionName));
        services.Configure<SubredditSettings>(configuration.GetSection(SubredditSettings.SectionName));
        services.PostConfigure<SubredditSettings>(settings =>
        {
            var subreddits = configuration.GetSection(SubredditSettings.SectionName).Get<List<string>>();
            if (subreddits is not null && subreddits.Any())
                settings.Subreddits = subreddits;
        });

        // Dynamically build custom User Agent
        var platform = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        var applicationName = hostContext.HostingEnvironment.ApplicationName;
        var userName = configuration.GetSection(RedditAuthSettings.SectionName)["Username"];
        var userAgent = $"{platform}:{applicationName}:v1.0.0 (by /u/{userName})";
        services.AddSingleton(userAgent);

        // Configure Redis
        var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString")!;
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

        // Add HttpClients with Polly policies
        services.AddHttpClient<IRedditAuthenticationService, RedditAuthenticationService>()
            .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
            .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

        services.AddHttpClient<SubredditMonitor>()
            .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
            .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

        // Add other services
        services.AddScoped<IRedditRepository, RedisRepository>();
        services.AddSingleton<IRateLimitService, RateLimitService>();
        services.AddSingleton<IRedditAuthenticationService, RedditAuthenticationService>();
        services.AddTransient<SubredditMonitor>();

        // Add hosted service
        services.AddHostedService<SubredditMonitorHostedService>();
    })
    .UseSerilog()
    .Build();

host.Run();