using System.Text.Json;
using Microsoft.Extensions.Logging;
using SdmCo.Reddit.Common.Entities;
using SdmCo.Reddit.Common.Exceptions;
using StackExchange.Redis;

namespace SdmCo.Reddit.Common.Persistence;

public class RedisRepository : IRedditRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRepository> _logger;

    public RedisRepository(IConnectionMultiplexer redis, ILogger<RedisRepository> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    // Use a Redis set to store unique posts for each subreddit.
    // A Redis set provides O(1) complexity for insert, remove, and lookup operations
    public async Task AddPostsAsync(string subreddit, List<RedditPost> posts)
    {
        _logger.LogInformation("Adding {NewPostCount} new posts in {SubredditName} to Redis.", posts.Count, subreddit);
        var db = _redis.GetDatabase();

        foreach (var post in posts)
        {
            var serializedPost = JsonSerializer.Serialize(post);

            await db.SetAddAsync(subreddit, serializedPost);
        }
    }

    // Deserialize the posts for a given subreddit and check to see which one has the most Ups (upvotes)
    public async Task<RedditPost> GetMostUpvotedPostAsync(string subreddit)
    {
        var db = _redis.GetDatabase();

        var postSet = await db.SetMembersAsync(subreddit);

        if (postSet.Length == 0)
            throw new SubredditNotFoundException(subreddit);

        var posts = postSet.Select(p => JsonSerializer.Deserialize<RedditPost>(p!)).ToList();

        var mostUpvotedPost = posts.MaxBy(p => p!.Ups)!;

        _logger.LogInformation("Calculated {MostUpvotedPostName} as most upvoted new post in {SubredditName}", mostUpvotedPost.Title, subreddit);
        
        return mostUpvotedPost!;
    }

    // Deserialize the posts for a given subreddit, group by author, and see which author has the most posts
    public async Task<RedditUser> GetUserWithMostPostsAsync(string subreddit)
    {
        var db = _redis.GetDatabase();

        var postSet = await db.SetMembersAsync(subreddit);

        if (postSet.Length == 0)
            throw new SubredditNotFoundException(subreddit);

        var posts = postSet.Select(p => JsonSerializer.Deserialize<RedditPost>(p!)).ToList();

        var mostActiveUserGroup = posts.GroupBy(p => p!.Author).MaxBy(g => g.Count());

        var mostActiveUsername = mostActiveUserGroup?.Key ?? string.Empty;
        var mostActiveUserPostCount = mostActiveUserGroup?.Count() ?? 0;

        _logger.LogInformation(
            "Calculated {MostActiveUser} as the user with the most posts ({PostCount}) in {SubredditName}",
            mostActiveUsername, mostActiveUserPostCount, subreddit);

        return new RedditUser
        {
            Username = mostActiveUsername,
            PostCount = mostActiveUserPostCount
        };
    }
}