namespace SdmCo.Reddit.Common.Entities;

public record RedditAuthToken(string AccessToken, int ExpiresIn, DateTime AquiredAt)
{
    public DateTime Expiry => AquiredAt.AddSeconds(ExpiresIn);
}