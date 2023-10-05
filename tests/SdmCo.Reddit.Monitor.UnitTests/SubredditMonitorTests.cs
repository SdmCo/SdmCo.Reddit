using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SdmCo.Reddit.Common.Entities;
using SdmCo.Reddit.Common.Persistence;
using SdmCo.Reddit.Monitor.Monitors;
using SdmCo.Reddit.Monitor.Services;
using SdmCo.Reddit.Monitor.UnitTests.Mocks;

namespace SdmCo.Reddit.Monitor.UnitTests;

public class SubredditMonitorTests
{
    [Fact]
    public void Test_Initialization()
    {
        var redditAuthService = new Mock<IRedditAuthenticationService>();
        var redditRepo = new Mock<IRedditRepository>();
        var rateLimitService = new Mock<IRateLimitService>();
        var logger = new Mock<ILogger<SubredditMonitor>>();
        var httpClient = new HttpClient();

        var subredditMonitor = new SubredditMonitor(
            redditAuthService.Object,
            rateLimitService.Object,
            httpClient,
            redditRepo.Object,
            logger.Object
        );

        Assert.NotNull(subredditMonitor);
    }

    [Fact]
    public async Task FetchNewPostsAsync_Successful()
    {
        var jsonResponse = """
            {"kind": "Listing", "data": {"after": "t3_16yxnnf", "dist": 1, "modhash": null, "geo_filter": "", "children": [{"kind": "t3", "data": {"approved_at_utc": null, "subreddit": "programming", "selftext": "", "author_fullname": "t2_90mxj2oz", "saved": false, "mod_reason_title": null, "gilded": 0, "clicked": false, "title": "Allowing DML Operations in Highly Compressed Data in Postgres", "link_flair_richtext": [], "subreddit_name_prefixed": "r/programming", "hidden": false, "pwls": 6, "link_flair_css_class": null, "downs": 0, "top_awarded_type": null, "hide_score": true, "name": "t3_16yxnnf", "quarantine": false, "link_flair_text_color": "dark", "upvote_ratio": 1.0, "author_flair_background_color": null, "subreddit_type": "public", "ups": 1, "total_awards_received": 0, "media_embed": {}, "author_flair_template_id": null, "is_original_content": false, "user_reports": [], "secure_media": null, "is_reddit_media_domain": false, "is_meta": false, "category": null, "secure_media_embed": {}, "link_flair_text": null, "can_mod_post": false, "score": 1, "approved_by": null, "is_created_from_ads_ui": false, "author_premium": false, "thumbnail": "", "edited": false, "author_flair_css_class": null, "author_flair_richtext": [], "gildings": {}, "content_categories": null, "is_self": false, "mod_note": null, "created": 1696353301.0, "link_flair_type": "text", "wls": 6, "removed_by_category": null, "banned_by": null, "author_flair_type": "text", "domain": "timescale.com", "allow_live_comments": false, "selftext_html": null, "likes": null, "suggested_sort": null, "banned_at_utc": null, "url_overridden_by_dest": "https://www.timescale.com/blog/allowing-dml-operations-in-highly-compressed-time-series-data-in-postgresql/", "view_count": null, "archived": false, "no_follow": true, "is_crosspostable": true, "pinned": false, "over_18": false, "all_awardings": [], "awarders": [], "media_only": false, "can_gild": false, "spoiler": false, "locked": false, "author_flair_text": null, "treatment_tags": [], "visited": false, "removed_by": null, "num_reports": null, "distinguished": null, "subreddit_id": "t5_2fwo", "author_is_blocked": false, "mod_reason_by": null, "removal_reason": null, "link_flair_background_color": "", "id": "16yxnnf", "is_robot_indexable": true, "report_reasons": null, "author": "carlotasoto", "discussion_type": null, "num_comments": 0, "send_replies": true, "whitelist_status": "all_ads", "contest_mode": false, "mod_reports": [], "author_patreon_flair": false, "author_flair_text_color": null, "permalink": "/r/programming/comments/16yxnnf/allowing_dml_operations_in_highly_compressed_data/", "parent_whitelist_status": "all_ads", "stickied": false, "url": "https://www.timescale.com/blog/allowing-dml-operations-in-highly-compressed-time-series-data-in-postgresql/", "subreddit_subscribers": 5631346, "created_utc": 1696353301.0, "num_crossposts": 0, "media": null, "is_video": false}}], "before": null}}
            """;

        var redditAuthService = new Mock<IRedditAuthenticationService>();
        var redditRepo = new Mock<IRedditRepository>();
        var rateLimitService = new Mock<IRateLimitService>();
        var logger = new Mock<ILogger<SubredditMonitor>>();

        var mockHttpMessageHandler = new MockHttpMessageHandler((request, cancellationToken) =>
            new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler);

        redditAuthService.Setup(m => m.GetValidTokenAsync()).ReturnsAsync("dummy_token");
        rateLimitService.Setup(m => m.CanMakeRequest()).Returns(true);

        var subredditMonitor = new SubredditMonitor(
            redditAuthService.Object,
            rateLimitService.Object,
            httpClient,
            redditRepo.Object,
            logger.Object
        );

        subredditMonitor.ConfigureSubreddit("test_subreddit");

        // MonitorAsync will run forever, for testing purposes we do not want that.
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var monitorTask = subredditMonitor.MonitorAsync(cts.Token);
        var completedTask = await Task.WhenAny(monitorTask, Task.Delay(10000, cts.Token));


        redditRepo.Verify(m => m.AddPostsAsync("test_subreddit", It.IsAny<List<RedditPost>>()), Times.Once);
    }

    [Fact]
    public async Task FetchNewPostsAsync_Failure()
    {
        var redditAuthService = new Mock<IRedditAuthenticationService>();
        var redditRepo = new Mock<IRedditRepository>();
        var rateLimitService = new Mock<IRateLimitService>();
        var logger = new Mock<ILogger<SubredditMonitor>>();

        // Mock an HttpMessageHandler to return a failure response.
        var mockHttpMessageHandler = new MockHttpMessageHandler((request, cancellationToken) =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(mockHttpMessageHandler);

        redditAuthService.Setup(m => m.GetValidTokenAsync()).ReturnsAsync("dummy_token");
        rateLimitService.Setup(m => m.CanMakeRequest()).Returns(true);

        var subredditMonitor = new SubredditMonitor(
            redditAuthService.Object,
            rateLimitService.Object,
            httpClient,
            redditRepo.Object,
            logger.Object
        );

        subredditMonitor.ConfigureSubreddit("test_subreddit");

        // MonitorAsync will run forever, for testing purposes we do not want that.
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var monitorTask = subredditMonitor.MonitorAsync(cancellationTokenSource.Token);
        var completedTask = await Task.WhenAny(monitorTask, Task.Delay(10000, cancellationTokenSource.Token));

        redditRepo.Verify(m => m.AddPostsAsync("test_subreddit", It.IsAny<List<RedditPost>>()), Times.Never);
    }

    [Fact]
    public async Task RateLimitReached_WaitForReset()
    {
        var redditAuthService = new Mock<IRedditAuthenticationService>();
        var redditRepo = new Mock<IRedditRepository>();
        var rateLimitService = new Mock<IRateLimitService>();
        var logger = new Mock<ILogger<SubredditMonitor>>();
        var httpClient = new Mock<HttpClient>();

        redditAuthService.Setup(m => m.GetValidTokenAsync()).ReturnsAsync("dummy_token");
        rateLimitService.Setup(m => m.CanMakeRequest()).Returns(false);
        rateLimitService.Setup(m => m.WaitForResetAsync()).Returns(Task.CompletedTask);

        var subredditMonitor = new SubredditMonitor(
            redditAuthService.Object,
            rateLimitService.Object,
            httpClient.Object,
            redditRepo.Object,
            logger.Object
        );

        subredditMonitor.ConfigureSubreddit("test_subreddit");

        // Using CancellationTokenSource to cancel after a short time to simulate long-running task
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await subredditMonitor.MonitorAsync(cts.Token);

        rateLimitService.Verify(m => m.WaitForResetAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task MonitorAsync_AuthenticationFailure()
    {
        // Arrange
        var redditAuthService = new Mock<IRedditAuthenticationService>();
        var redditRepo = new Mock<IRedditRepository>();
        var rateLimitService = new Mock<IRateLimitService>();
        var logger = new Mock<ILogger<SubredditMonitor>>();

        redditAuthService.Setup(m => m.GetValidTokenAsync()).ReturnsAsync("dummy_token");
        rateLimitService.Setup(m => m.CanMakeRequest()).Returns(true);

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpResponseMessage = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("Bad request")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        var subredditMonitor = new SubredditMonitor(
            redditAuthService.Object,
            rateLimitService.Object,
            httpClient,
            redditRepo.Object,
            logger.Object
        );

        subredditMonitor.ConfigureSubreddit("test_subreddit");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<HttpRequestException>(() => subredditMonitor.MonitorAsync(cts.Token));
    }
}