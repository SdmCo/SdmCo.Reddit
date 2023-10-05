using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SdmCo.Reddit.Monitor.Services;
using SdmCo.Reddit.Monitor.Settings;
using SdmCo.Reddit.Monitor.UnitTests.Mocks;

namespace SdmCo.Reddit.Monitor.UnitTests;

public class RedditAuthenticationServiceTests
{
    [Fact]
    public async Task Test_Authentication_Success()
    {
        // Arrange
        var authSettings = new RedditAuthSettings();
        var mockOptions = new Mock<IOptions<RedditAuthSettings>>();
        mockOptions.Setup(ap => ap.Value).Returns(authSettings);

        var logger = new Mock<ILogger<RedditAuthenticationService>>();

        var mockHttpMessageHandler = new MockHttpMessageHandler((request, cancellationToken) =>
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK, Content = new StringContent("{ \"access_token\": \"token_here\" }")
            });
        var httpClient = new HttpClient(mockHttpMessageHandler);

        var authService = new RedditAuthenticationService(mockOptions.Object, httpClient, logger.Object);

        // Act
        var token = await authService.GetValidTokenAsync();

        // Assert
        Assert.Equal("token_here", token);
    }

    [Fact]
    public async Task Test_Authentication_Unauthorized()
    {
        // Arrange
        var authSettings = new RedditAuthSettings();
        var mockOptions = new Mock<IOptions<RedditAuthSettings>>();
        mockOptions.Setup(ap => ap.Value).Returns(authSettings);

        var logger = new Mock<ILogger<RedditAuthenticationService>>();

        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
            new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized });
        var httpClient = new HttpClient(handler);

        var authService = new RedditAuthenticationService(mockOptions.Object, httpClient, logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => authService.GetValidTokenAsync());
    }
}