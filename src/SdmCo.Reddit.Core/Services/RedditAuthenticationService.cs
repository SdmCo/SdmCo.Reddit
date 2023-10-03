using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SdmCo.Reddit.Core.Entities;
using SdmCo.Reddit.Core.Entities.Dtos;
using SdmCo.Reddit.Core.Settings;

namespace SdmCo.Reddit.Core.Services;

public class RedditAuthenticationService : IRedditAuthenticationService
{
    private const string AccessTokenUrl = "https://www.reddit.com/api/v1/access_token";
    private const string UserAgent = ".NET 7:SdmCoStats:v1.0.0 (by /u/Calamity_Rainbow)";

    private readonly RedditAuthSettings _config;
    private readonly HttpClient _httpClient;

    private RedditAuthToken? _currentToken;

    public RedditAuthenticationService(IOptions<RedditAuthSettings> config, HttpClient httpClient)
    {
        _config = config.Value;
        _httpClient = httpClient;
    }

    public async Task<string> GetValidTokenAsync()
    {
        if (_currentToken == null || DateTime.UtcNow >= _currentToken.Expiry)
        {
            var tokenResponse = await GetAccessTokenAsync();
            _currentToken = RedditAuthToken.FromTokenResponse(tokenResponse);
        }

        return _currentToken.AccessToken;
    }

    private async Task<TokenResponse> GetAccessTokenAsync()
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, AccessTokenUrl);
        requestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));

        var byteArray = Encoding.ASCII.GetBytes($"{_config.ClientId}:{_config.ClientSecret}");
        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", _config.Username),
            new KeyValuePair<string, string>("password", _config.Password)
        });

        requestMessage.Content = content;

        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<TokenResponse>(jsonResponse)!;
    }
}