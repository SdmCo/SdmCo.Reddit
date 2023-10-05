using System.Text.Json;
using SdmCo.Reddit.Common.Entities;
using StackExchange.Redis;

namespace SdmCo.Reddit.Common.Persistence;

public class RedisRepository : IRedditRepository
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRepository(IConnectionMultiplexer redis) => _redis = redis;

    // Use a Redis set to store unique posts for each subreddit.
    // A Redis set provides O(1) complexity for insert, remove, and lookup operations
    public async Task AddPostsAsync(string subreddit, List<RedditPost> posts)
    {
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
        var posts = postSet.Select(p => JsonSerializer.Deserialize<RedditPost>(p!)).ToList();

        var mostUpvotedPost = posts.MaxBy(p => p!.Ups);
        
        return mostUpvotedPost!;
    }

    // Deserialize the posts for a given subreddit, group by author, and see which author has the most posts
    public async Task<RedditUser> GetUserWithMostPostsAsync(string subreddit)
    {
        var db = _redis.GetDatabase();

        var postSet = await db.SetMembersAsync(subreddit);
        var posts = postSet.Select(p => JsonSerializer.Deserialize<RedditPost>(p!)).ToList();

        var mostActiveUserGroup = posts.GroupBy(p => p!.Author).MaxBy(g => g.Count());

        var mostActiveUsername = mostActiveUserGroup?.Key ?? string.Empty;
        var mostActiveUserPostCount = mostActiveUserGroup?.Count() ?? 0;

        return new RedditUser
        {
            Username = mostActiveUsername,
            PostCount = mostActiveUserPostCount
        };
    }
}