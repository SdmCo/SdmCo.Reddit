using SdmCo.Reddit.Common.Entities;

namespace SdmCo.Reddit.Common.Persistence;

public interface IRedditRepository
{
    Task AddPostsAsync(string subreddit, List<RedditPost> posts);
    Task<RedditPost> GetMostUpvotedPostAsync(string subreddit);
    Task<RedditUser> GetUserWithMostPostsAsync(string subreddit);
}