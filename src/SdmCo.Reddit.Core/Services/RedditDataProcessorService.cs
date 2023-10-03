using System.Text.Json;
using SdmCo.Reddit.Core.Entities.Dtos;
using StackExchange.Redis;

namespace SdmCo.Reddit.Core.Services;

public class RedditDataProcessorService : IRedditDataProcessorService
{
    private readonly IDatabase _db;

    public RedditDataProcessorService(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    public async Task ProcessDataAsync(string subredditName, List<RedditPostContainer> postContainers)
    {
        foreach (var postContainer in postContainers)
        {
            var post = postContainer.Data;
            var serializedPost = JsonSerializer.Serialize(post);

            await _db.SetAddAsync(subredditName, serializedPost);
        }
    }
}