using SdmCo.Reddit.Core.Entities.Dtos;

namespace SdmCo.Reddit.Core.Services;

public interface IRedditAuthenticationService
{
    Task<string> GetValidTokenAsync();
}