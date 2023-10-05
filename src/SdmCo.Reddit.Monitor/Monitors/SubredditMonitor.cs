using System.Net.Http.Headers;
using System.Text.Json;
using SdmCo.Reddit.Common.Entities;
using SdmCo.Reddit.Common.Persistence;
using SdmCo.Reddit.Monitor.Entities.Dtos;
using SdmCo.Reddit.Monitor.Services;

namespace SdmCo.Reddit.Monitor.Monitors;

public class SubredditMonitor
{
    // User agent for Reddit API requests
    private const string RedditUserAgent = "Windows:SdmCoStats:v1.0.0 (by /u/Calamity_Rainbow)";

    // Injected services and configuration settings
    private readonly IRedditAuthenticationService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubredditMonitor> _logger;
    private readonly IRateLimitService _rateLimitService;
    private readonly IRedditRepository _repository;

    // Configuration settings
    private readonly int _maxRetryCount = 5;

    // Keeps track of seen post IDs to monitor if a response inlcudes new posts
    private readonly HashSet<string> _seenPostIds = new();

    // Retry counter and subreddit to monitor
    private int _retryCount;
    private string _subreddit = string.Empty;

    public SubredditMonitor(IRedditAuthenticationService authService, IRateLimitService rateLimitService,
        HttpClient httpClient, IRedditRepository repository, ILogger<SubredditMonitor> logger)
    {
        _authService = authService;
        _repository = repository;
        _logger = logger;
        _rateLimitService = rateLimitService;

        _httpClient = httpClient;
    }

    public void ConfigureSubreddit(string subreddit) => _subreddit = subreddit;

    public async Task MonitorAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting monitoring for subreddit {SubredditName}", _subreddit);

        // Monitor subreddits until cancellation is requested
        while (!cancellationToken.IsCancellationRequested)
        {
            // Check the rate-limiting service before making a request
            if (!_rateLimitService.CanMakeRequest())
            {
                _logger.LogWarning("Rate Limit hit, pausing.");
                await _rateLimitService.WaitForResetAsync();
                continue;
            }

            // Auth token
            var token = await _authService.GetValidTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Add custom user agent
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", RedditUserAgent);

            // Make request and ensure its successful
            var response =
                await _httpClient.GetAsync($"https://oauth.reddit.com/r/{_subreddit}/new.json", cancellationToken);
            response.EnsureSuccessStatusCode();

            // Update rate-limiting service with new values
            if (response.Headers.Contains("X-RateLimit-Remaining") && response.Headers.Contains("X-RateLimit-Reset"))
            {
                var rateLimitRemaining = response.Headers.GetValues("X-RateLimit-Remaining").First();
                var resetInSeconds = response.Headers.GetValues("X-RateLimit-Reset").First();

                // Round rate limit remainign down to the nearest integer.
                var remaining = (int)Math.Floor(decimal.Parse(rateLimitRemaining));
                var reset = int.Parse(resetInSeconds);

                _rateLimitService.SetRateLimitInfo(remaining, reset);
            }

            // Deserialize response content
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _logger.LogInformation("Checking for new posts in {SubredditName}", _subreddit);

            var newPostsResponse = JsonSerializer.Deserialize<NewPostsResponse>(content, jsonOptions);

            if (newPostsResponse is null)
                continue;

            // Check if we have new posts in this response from the previous one
            var (hasNewPosts, newPostCount) = HasNewPosts(newPostsResponse);
            if (hasNewPosts)
            {
                _logger.LogInformation("{NewPostCount} new posts found in {SubredditName}", newPostCount, _subreddit);
                
                var redditPosts = newPostsResponse.Data.Children.Select(c => new RedditPost
                {
                    Author = c.Data.Author,
                    Title = c.Data.Title,
                    Ups = c.Data.Ups
                }).ToList();
                
                // Add posts to Redis
                await _repository.AddPostsAsync(_subreddit, redditPosts);

                // Reset retry count
                _retryCount = 0;
            }
            else
            {
                // No new posts in this response
                if (_retryCount < _maxRetryCount)
                    _retryCount++;
                
                // Exponential delay (2^_retryCount)  This will max out at 2^_maxRetryCount until new posts are found.
                var delay = TimeSpan.FromSeconds(Math.Pow(2, _retryCount));
                _logger.LogInformation(
                    "No new posts found in {SubredditName}.  Waiting {Delay} seconds until trying again.", _subreddit,
                    delay);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private (bool hasNewPosts, int newPostCount) HasNewPosts(NewPostsResponse response)
    {
        var hasNewPosts = false;
        var newPostCount = 0;

        // Loop through new posts 
        // HashSet.Add() will return false if the Id already exists and true if it does not
        foreach (var post in response.Data.Children)
            if (_seenPostIds.Add(post.Data.Id))
            {
                hasNewPosts = true;
                newPostCount++;
            }

        return (hasNewPosts, newPostCount);
    }
}