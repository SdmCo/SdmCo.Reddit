namespace SdmCo.Reddit.Monitor.Entities.Dtos;

public record PostListingData(
    string After,
    int Dist,
    string? Modhash,
    string GeoFilter,
    List<Post> Children
);