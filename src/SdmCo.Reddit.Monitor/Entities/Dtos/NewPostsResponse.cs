namespace SdmCo.Reddit.Monitor.Entities.Dtos;

public record NewPostsResponse(
    string Kind,
    PostListingData Data
);