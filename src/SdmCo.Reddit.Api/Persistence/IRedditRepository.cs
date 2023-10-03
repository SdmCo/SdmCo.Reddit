using SdmCo.Reddit.Api.Entities;
using SdmCo.Reddit.Api.Entities.Dtos;

namespace SdmCo.Reddit.Api.Persistence;

public interface IRedditRepository
{
    Task AddPostsAsync(string subreddit, List<Post> posts);
    Task<RedditPost> GetMostUpvotedPostAsync(string subreddit);
    Task<RedditUser> GetUserWithMostPostsAsync(string subreddit);
}