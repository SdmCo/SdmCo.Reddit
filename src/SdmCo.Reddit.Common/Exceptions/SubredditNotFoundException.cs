namespace SdmCo.Reddit.Common.Exceptions;

public class SubredditNotFoundException : Exception
{
    public SubredditNotFoundException(string subreddit) : base($"Subreddit {subreddit} not found.")
    {

    }
}