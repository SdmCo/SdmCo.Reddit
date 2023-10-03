namespace SdmCo.Reddit.Core.Settings;

public class SubredditSettings
{
    public const string SectionName = "RedditSubreddits";

    public List<string> Subreddits { get; init; } = new();
}