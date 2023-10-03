using SdmCo.Reddit.Core.Entities.Dtos;

namespace SdmCo.Reddit.Core.Services;

public interface IRedditDataProcessorService
{
    Task ProcessDataAsync(string subredditName, List<RedditPostContainer> postContainers);
}