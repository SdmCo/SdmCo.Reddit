namespace SdmCo.Reddit.Common.Entities;

public class RedditPost
{
    public string Title { get; set; } = default!;
    public int Ups { get; set; }
    public string Author { get; set; } = default!;
}