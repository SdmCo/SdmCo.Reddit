using System.Net.Http.Headers;
using System.Text.Json;
using SdmCo.Reddit.Api.Entities.Dtos;
using SdmCo.Reddit.Api.Persistence;
using SdmCo.Reddit.Api.Services;

namespace SdmCo.Reddit.Api.Monitors;

public class SubredditMonitor
{
    private const string RedditUserAgent = "Windows:SdmCoStats:v1.0.0 (by /u/Calamity_Rainbow)";

    private readonly IRedditAuthenticationService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubredditMonitor> _logger;
    private readonly int _maxRetryCount = 5;
    private readonly IRateLimitService _rateLimitService;
    private readonly IRedditRepository _repository;

    private readonly HashSet<string> _seenPostIds = new();

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

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_rateLimitService.CanMakeRequest())
            {
                _logger.LogWarning("Rate Limit hit, pausing.");
                await _rateLimitService.WaitForResetAsync();
                continue;
            }

            var token = await _authService.GetValidTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", RedditUserAgent);

            var response =
                await _httpClient.GetAsync($"https://oauth.reddit.com/r/{_subreddit}/new.json", cancellationToken);
            response.EnsureSuccessStatusCode();

            if (response.Headers.Contains("X-RateLimit-Remaining") && response.Headers.Contains("X-RateLimit-Reset"))
            {
                var rateLimitRemaining = response.Headers.GetValues("X-RateLimit-Remaining").First();
                var resetInSeconds = response.Headers.GetValues("X-RateLimit-Reset").First();

                // Round rate limit remainign down to the nearest integer.
                var remaining = (int)Math.Floor(decimal.Parse(rateLimitRemaining));
                var reset = int.Parse(resetInSeconds);

                _rateLimitService.SetRateLimitInfo(remaining, reset);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _logger.LogInformation("Checking for new posts in {SubredditName}", _subreddit);

            var newPostsResponse = JsonSerializer.Deserialize<NewPostsResponse>(content, jsonOptions);

            if (newPostsResponse is null)
                continue;

            var hasNewPost = HasNewPosts(newPostsResponse);
            if (hasNewPost)
            {
                _logger.LogInformation("New posts found in {SubredditName}", _subreddit);
                await _repository.AddPostsAsync(_subreddit, newPostsResponse.Data.Children);

                _retryCount = 0;
            }
            else
            {
                if (_retryCount < _maxRetryCount)
                    _retryCount++;

                var delay = TimeSpan.FromSeconds(Math.Pow(2, _retryCount));
                _logger.LogInformation(
                    "No new posts found in {SubredditName}.  Waiting {Delay} seconds until trying again.", _subreddit,
                    delay);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private bool HasNewPosts(NewPostsResponse response)
    {
        var hasNewPosts = false;

        foreach (var post in response.Data.Children)
            if (!_seenPostIds.Contains(post.Data.Id))
            {
                _seenPostIds.Add(post.Data.Id);
                hasNewPosts = true;
            }

        return hasNewPosts;
    }
}