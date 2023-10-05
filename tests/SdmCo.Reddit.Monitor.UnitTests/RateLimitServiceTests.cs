using SdmCo.Reddit.Monitor.Services;

namespace SdmCo.Reddit.Monitor.UnitTests;

public class RateLimitServiceTests
{
    private readonly RateLimitService _rateLimitService;

    public RateLimitServiceTests()
    {
        _rateLimitService = new RateLimitService();
    }

    [Fact]
    public void CanMakeRequest_ReturnsTrue_WhenUnderRateLimit()
    {
        _rateLimitService.SetRateLimitInfo(600, 10);
        Assert.True(_rateLimitService.CanMakeRequest());
    }

    [Fact]
    public void CanMakeRequest_ReturnsFalse_WhenRateLimitReached()
    {
        _rateLimitService.SetRateLimitInfo(0, 10);
        Assert.False(_rateLimitService.CanMakeRequest());
    }

    [Fact]
    public async Task WaitForResetAsync_WaitsForResetTime()
    {
        _rateLimitService.SetRateLimitInfo(0, 2);

        var start = DateTime.UtcNow;
        await _rateLimitService.WaitForResetAsync();
        var end = DateTime.UtcNow;

        Assert.True(end >= start.AddSeconds(2));
    }
}