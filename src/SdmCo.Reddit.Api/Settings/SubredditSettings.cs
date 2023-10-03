namespace SdmCo.Reddit.Api.Settings;

public class SubredditSettings
{
    public const string SectionName = "Subreddits";

    public List<string> Subreddits { get; set; } = new();
}