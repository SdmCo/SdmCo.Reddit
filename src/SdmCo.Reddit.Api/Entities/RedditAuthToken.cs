using SdmCo.Reddit.Api.Entities.Dtos;

namespace SdmCo.Reddit.Api.Entities;

public record RedditAuthToken(string AccessToken, int ExpiresIn, DateTime AquiredAt)
{
    public DateTime Expiry => AquiredAt.AddSeconds(ExpiresIn);

    public static RedditAuthToken FromTokenResponse(AccessTokenResponse accessTokenResponse) =>
        new(accessTokenResponse.AccessToken, accessTokenResponse.ExpiresIn, DateTime.UtcNow);
}