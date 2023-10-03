namespace SdmCo.Reddit.Api.Entities.Dtos;

public record StatisticsResponse(string SubredditName, MostUpvotedPost MostUpvotedPost, UserWithMostPosts UserWithMostPosts);