using System.Net.Http.Headers;
using System.Text.Json;
using Polly;
using Polly.Retry;
using SdmCo.Reddit.Core.Entities.Dtos;
using SdmCo.Reddit.Core.Services;

namespace SdmCo.Reddit.Core.Monitors;

public class SubredditMonitor
{
    private readonly IRedditAuthenticationService _authService;
    private readonly IRedditDataProcessorService _dataProcessorService;
    private readonly HttpClient _httpClient;
    private readonly IRateLimitService _rateLimitService;
    private readonly string _subreddit;
    private string _lastPostId = string.Empty;
    private readonly AsyncRetryPolicy<bool> _backoffPolicy;


    public SubredditMonitor(IRedditAuthenticationService authService, IRateLimitService rateLimitService, 
        HttpClient httpClient, IRedditDataProcessorService dataProcessorService, string subreddit)
    {
        _authService = authService;
        _subreddit = subreddit;
        _rateLimitService = rateLimitService;
        _httpClient = httpClient;
        _dataProcessorService = dataProcessorService;

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

    public async Task MonitorAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_rateLimitService.CanMakeRequest())
            {
                await _rateLimitService.WaitForResetAsync();
                continue;
            }

            var token = await _authService.GetValidTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
            var newPostsResponse = JsonSerializer.Deserialize<NewPostResponse>(content);

            if (newPostsResponse is null) 
                continue;

            var hasNewPost = HasNewPosts(newPostsResponse);
            if (hasNewPost)
                await _dataProcessorService.ProcessDataAsync(_subreddit, newPostsResponse.Data.Children);

            await _backoffPolicy.ExecuteAsync(() => Task.FromResult(hasNewPost));
        }
    }

    private bool HasNewPosts(NewPostResponse response)
    {
        foreach (var post in response.Data.Children)
            if (post.Data.Id != _lastPostId)
            {
                _lastPostId = post.Data.Id;
                return true;
            }

        return false;
    }
}