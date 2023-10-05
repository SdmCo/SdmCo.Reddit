using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SdmCo.Reddit.Common.Entities;
using SdmCo.Reddit.Monitor.Entities.Dtos;
using SdmCo.Reddit.Monitor.Settings;

namespace SdmCo.Reddit.Monitor.Services;

public class RedditAuthenticationService : IRedditAuthenticationService
{
    // Auth endpoint and custom user agent
    private const string AccessTokenUrl = "https://www.reddit.com/api/v1/access_token";
    private const string RedditUserAgent = "Windows:SdmCoStats:v1.0.0 (by /u/Calamity_Rainbow)";

    private readonly RedditAuthSettings _authSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RedditAuthenticationService> _logger;

    // Current token
    private RedditAuthToken? _currentToken;

    public RedditAuthenticationService(IOptions<RedditAuthSettings> authSettings, HttpClient httpClient,
        ILogger<RedditAuthenticationService> logger)
    {
        _authSettings = authSettings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    // Either return our current token if its valid, or try and get a new one
    public async Task<string> GetValidTokenAsync()
    {
        if (_currentToken == null || DateTime.UtcNow >= _currentToken.Expiry)
        {
            _logger.LogInformation("Current auth token does not exist or has expired.  Retrieving new auth token.");
            var tokenResponse = await GetAccessTokenAsync();
            _currentToken = tokenResponse.ToAuthToken();
        }

        return _currentToken.AccessToken;
    }

    // Get an access token.  Reddit does not appear to use refresh tokens anymore, so a full login is required each time
    private async Task<AccessTokenResponse> GetAccessTokenAsync()
    {
        _logger.LogInformation("Requesting new auth token.");
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, AccessTokenUrl);

        var byteArray = Encoding.ASCII.GetBytes($"{_authSettings.ClientId}:{_authSettings.ClientSecret}");
        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", _authSettings.Username),
            new KeyValuePair<string, string>("password", _authSettings.Password)
        });

        requestMessage.Content = content;

        requestMessage.Headers.TryAddWithoutValidation("User-Agent", RedditUserAgent);

        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("New auth token received.");

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<AccessTokenResponse>(jsonResponse, jsonOptions)!;
    }
}