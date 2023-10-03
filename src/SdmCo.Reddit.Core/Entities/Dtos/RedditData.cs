namespace SdmCo.Reddit.Core.Entities.Dtos;

public record RedditData(string After, List<RedditPostContainer> Children);