using System.Text.Json.Serialization;
using SdmCo.Reddit.Common.Entities;

namespace SdmCo.Reddit.Monitor.Entities.Dtos;

public record AccessTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = default!;

    [JsonPropertyName("token_type")] public string TokenType { get; set; } = default!;

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")] public string Scope { get; set; } = default!;

    public RedditAuthToken ToAuthToken() => new(AccessToken, ExpiresIn, DateTime.UtcNow);
}