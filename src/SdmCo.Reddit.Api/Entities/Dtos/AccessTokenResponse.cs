using System.Text.Json.Serialization;

namespace SdmCo.Reddit.Api.Entities.Dtos;

public record AccessTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = default!;

    [JsonPropertyName("token_type")] public string TokenType { get; set; } = default!;

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")] public string Scope { get; set; } = default!;
}