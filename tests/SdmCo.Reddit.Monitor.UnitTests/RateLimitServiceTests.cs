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
        // Arrange
        _rateLimitService.SetRateLimitInfo(600, 10);
        
        // Act & Assert
        Assert.True(_rateLimitService.CanMakeRequest());
    }

    [Fact]
    public void CanMakeRequest_ReturnsFalse_WhenRateLimitReached()
    {
        // Arrange
        _rateLimitService.SetRateLimitInfo(0, 10);
        
        // Act & Assert
        Assert.False(_rateLimitService.CanMakeRequest());
    }

    [Fact]
    public async Task WaitForResetAsync_WaitsForResetTime()
    {
        // Arrange
        _rateLimitService.SetRateLimitInfo(0, 2);

        // Act
        var start = DateTime.UtcNow;
        await _rateLimitService.WaitForResetAsync();
        var end = DateTime.UtcNow;

        // Assert
        Assert.True(end >= start.AddSeconds(2));
    }
}