namespace SdmCo.Reddit.Api.Entities.Dtos;

public record AccessTokenResponse(string AccessToken,
    string TokenType,
    int ExpiresIn,
    string Scope);