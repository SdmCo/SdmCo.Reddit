using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SdmCo.Reddit.Core.Services;
using SdmCo.Reddit.Core.Settings;

namespace SdmCo.Reddit.Tests.Integration;

public class IntegrationTestFixture : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public IntegrationTestFixture()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddUserSecrets<IntegrationTestFixture>();

        _configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddHttpClient<IRedditAuthenticationService, RedditAuthenticationService>();
        services.Configure<RedditAuthSettings>(_configuration.GetSection(RedditAuthSettings.RedditAuth);

        var serviceProvider = services.BuildServiceProvider();
        _httpClient = serviceProvider.GetRequiredService<HttpClient>();
    }

    public HttpClient HttpClient => _httpClient;

    public IOptions<RedditAuthSettings> RedditAuthSettings => serviceProvider

    public void Dispose() => _httpClient.Dispose();
}