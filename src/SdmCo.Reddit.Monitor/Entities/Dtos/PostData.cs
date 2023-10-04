namespace SdmCo.Reddit.Monitor.Entities.Dtos;

public record PostData(
    string Id,
    string Title,
    int Ups,
    string Author
);