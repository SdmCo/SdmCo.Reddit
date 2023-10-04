namespace SdmCo.Reddit.Monitor.Services;

public interface IRedditAuthenticationService
{
    Task<string> GetValidTokenAsync();
}