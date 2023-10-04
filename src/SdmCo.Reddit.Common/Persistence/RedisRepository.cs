using System.Text.Json;
using SdmCo.Reddit.Common.Entities;
using StackExchange.Redis;

namespace SdmCo.Reddit.Common.Persistence;

public class RedisRepository : IRedditRepository
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRepository(IConnectionMultiplexer redis) => _redis = redis;

    public async Task AddPostsAsync(string subreddit, List<RedditPost> posts)
    {
        var db = _redis.GetDatabase();

        foreach (var post in posts)
        {
            var serializedPost = JsonSerializer.Serialize(post);

            await db.SetAddAsync(subreddit, serializedPost);
        }
    }

    public async Task<RedditPost> GetMostUpvotedPostAsync(string subreddit)
    {
        var db = _redis.GetDatabase();

        var postSet = await db.SetMembersAsync(subreddit);
        var posts = postSet.Select(p => JsonSerializer.Deserialize<RedditPost>(p!)).ToList();

        var mostUpvotedPost = posts.MaxBy(p => p!.Ups);
        
        return mostUpvotedPost!;
    }

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