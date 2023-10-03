using System.Net.Http.Headers;
using System.Text.Json;
using Polly;
using Polly.Retry;
using SdmCo.Reddit.Api.Entities.Dtos;
using SdmCo.Reddit.Api.Persistence;
using SdmCo.Reddit.Api.Services;

namespace SdmCo.Reddit.Api.Monitors;

public class SubredditMonitor
{
    private const string RedditUserAgent = "Windows:SdmCoStats:v1.0.0 (by /u/Calamity_Rainbow)";

    private readonly IRedditAuthenticationService _authService;
    private readonly IRedditRepository _repository;
    private readonly HttpClient _httpClient;
    private readonly IRateLimitService _rateLimitService;
    private string _subreddit = string.Empty;
    private string _lastPostId = string.Empty;
    private readonly AsyncRetryPolicy<bool> _backoffPolicy;
    private readonly ILogger<SubredditMonitor> _logger;

    private HashSet<string> _seenPostIds = new HashSet<string>();

    public SubredditMonitor(IRedditAuthenticationService authService, IRateLimitService rateLimitService, 
        IHttpClientFactory httpClientFactory, IRedditRepository repository, ILogger<SubredditMonitor> logger)
    {
        _authService = authService;
        _repository = repository;
        _logger = logger;
        _rateLimitService = rateLimitService;

        _httpClient = httpClientFactory.CreateClient(nameof(SubredditMonitor));

        _backoffPolicy = Policy
            .HandleResult<bool>(r => r == false)
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(16),
            });
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
                await _httpClient.GetAsync($"https://www.reddit.com/r/{_subreddit}/new.json", cancellationToken);
            response.EnsureSuccessStatusCode();

            if (response.Headers.Contains("X-RateLimit-Remaining") && response.Headers.Contains("X-RateLimit-Reset"))
            {
                var remaining = int.Parse(response.Headers.GetValues("X-RateLimit-Remaining").First());
                var resetInSeconds = int.Parse(response.Headers.GetValues("X-RateLimit-Reset").First());

                _rateLimitService.SetRateLimitInfo(remaining, resetInSeconds);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var newPostsResponse = JsonSerializer.Deserialize<NewPostsResponse>(content, jsonOptions);

            if (newPostsResponse is null) 
                continue;

            var hasNewPost = HasNewPosts(newPostsResponse);
            if (hasNewPost)
            {
                _logger.LogInformation("New posts found for subreddit {SubredditName}", _subreddit);
                await _repository.AddPostsAsync(_subreddit, newPostsResponse.Data.Children);
            }

            await _backoffPolicy.ExecuteAsync(() => Task.FromResult(hasNewPost));

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    private bool HasNewPosts(NewPostsResponse response)
    {
        bool hasNewPosts = false;

        foreach (var post in response.Data.Children)
        {
            if (!_seenPostIds.Contains(post.Data.Id))
            {
                _seenPostIds.Add(post.Data.Id);
                hasNewPosts = true;
            }
        }

        return hasNewPosts;
    }
}