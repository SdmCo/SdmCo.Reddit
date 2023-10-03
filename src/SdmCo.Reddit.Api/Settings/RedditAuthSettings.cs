namespace SdmCo.Reddit.Api.Settings;

public class RedditAuthSettings
{
    public const string SectionName = "RedditAuth";

    public string ClientId { get; init; } = default!;
    public string ClientSecret { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
}