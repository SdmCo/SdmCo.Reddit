using SdmCo.Reddit.Api.Extensions;
using SdmCo.Reddit.Api.Monitors;
using SdmCo.Reddit.Api.Persistence;
using SdmCo.Reddit.Api.Policies;
using SdmCo.Reddit.Api.Services;
using SdmCo.Reddit.Api.Settings;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilog();

builder.Services.Configure<RedditAuthSettings>(builder.Configuration.GetSection(RedditAuthSettings.SectionName));

builder.Services.Configure<SubredditSettings>(builder.Configuration.GetSection(SubredditSettings.SectionName));
builder.Services.PostConfigure<SubredditSettings>(settings =>
{
    var subreddits = builder.Configuration.GetSection(SubredditSettings.SectionName).Get<List<string>>();
    if (subreddits is not null && subreddits.Any())
        settings.Subreddits = subreddits;
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString")!;

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddHttpClient<IRedditAuthenticationService, RedditAuthenticationService>()
    .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
    .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

builder.Services.AddHttpClient<SubredditMonitor>()
    .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
    .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

builder.Services.AddScoped<IRedditRepository, RedisRepository>();

builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
builder.Services.AddSingleton<IRedditAuthenticationService, RedditAuthenticationService>();
builder.Services.AddTransient<SubredditMonitor>();

builder.Services.AddHostedService<SubredditMonitorHostedService>();

var app = builder.Build();

app.Logger.LogInformation("{ApplicationName} created.", builder.Environment.ApplicationName);

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Logger.LogInformation("Starting application.");

app.Run();
