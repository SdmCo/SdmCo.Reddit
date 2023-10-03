namespace SdmCo.Reddit.Core.Entities.Dtos;

public record TokenResponse(string AccessToken, int ExpiresIn, string Scope, string TokenType);