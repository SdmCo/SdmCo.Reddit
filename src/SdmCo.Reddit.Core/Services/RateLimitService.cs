namespace SdmCo.Reddit.Core.Services;

public class RateLimitService : IRateLimitService
{
    private readonly SemaphoreSlim _semaphore = new(1);
    private int _remaining;
    private DateTime _resetTime;


    public void SetRateLimitInfo(int remaining, int resetInSeconds)
    {
        _remaining = remaining;
        _resetTime = DateTime.UtcNow.AddSeconds(resetInSeconds);
    }

    public bool CanMakeRequest()
    {
        if (_remaining > 0) 
            return true;

        return DateTime.UtcNow >= _resetTime;
    }

    public async Task WaitForResetAsync()
    {
        if (DateTime.UtcNow >= _resetTime) 
            return;

        await _semaphore.WaitAsync();
        try
        {
            var delay = _resetTime - DateTime.UtcNow;
            if (delay > TimeSpan.Zero) await Task.Delay(delay);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}