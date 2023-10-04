namespace SdmCo.Reddit.Monitor.Settings;

public class SubredditSettings
{
    public const string SectionName = "Subreddits";

    public List<string> Subreddits { get; set; } = new();
}