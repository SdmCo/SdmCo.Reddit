namespace SdmCo.Reddit.Api.Services;

public interface IRedditAuthenticationService
{
    Task<string> GetValidTokenAsync();
}