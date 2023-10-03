namespace SdmCo.Reddit.Api.Entities.Dtos;

public record NewPostsResponse(
    string Kind,
    PostListingData Data
);

public record PostListingData(
    string After,
    int Dist,
    string? Modhash,
    string GeoFilter,
    List<Post> Children
);

public record Post(
    string Kind,
    PostData Data
);

public record PostData(
    string Id,
    string Title,
    int Ups,
    string Author
);
