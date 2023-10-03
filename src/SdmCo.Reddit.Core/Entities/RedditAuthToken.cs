using SdmCo.Reddit.Core.Entities.Dtos;

namespace SdmCo.Reddit.Core.Entities;

public record RedditAuthToken
{
    public string AccessToken { get; init; } = default!;
    public int ExpiresIn { get; init; }
    public DateTime AquiredAt { get; init; }

    public DateTime Expiry => AquiredAt.AddSeconds(ExpiresIn);

    public static RedditAuthToken FromTokenResponse(TokenResponse tokenResponse) =>
        new()
        {
            AccessToken = tokenResponse.AccessToken,
            ExpiresIn = tokenResponse.ExpiresIn,
            AquiredAt = DateTime.UtcNow
        };
}