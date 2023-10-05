namespace SdmCo.Reddit.Monitor.Services;

public class RateLimitService : IRateLimitService
{
    /* Use a sempahore here to ensure only one monitor task will enter a wating state
       If we let every monitor task enter a wating state, then the moment the rate limit has been reset, they 
       will all try to make requests at once which may throw us right back into exceeding the rate limit.
       By only putting one task into a waiting state, when it is released, the other tasks will proceed to 
       aquire the semaphore and check the rate limit again, effectively staggering the requests.
    */
    private readonly SemaphoreSlim _semaphore = new(1);

    // How many requests are left until we hit the rate limit
    private int _remaining;

    // UTC time when the rate limit will be reset
    private DateTime _resetTime;

    // Set the current rate limit info
    // This is returned from reddit in each fetch new posts response
    public void SetRateLimitInfo(int remaining, int resetInSeconds)
    {
        _remaining = remaining;
        _resetTime = DateTime.UtcNow.AddSeconds(resetInSeconds);
    }

    // Determines if a new HTTP request can be made
    public bool CanMakeRequest()
    {
        // We still have remaining requests
        if (_remaining > 0) 
            return true;

        // Has the reset time been reached
        return DateTime.UtcNow >= _resetTime;
    }

    // If CanMakeReset returned false, we need stop operations
    public async Task WaitForResetAsync()
    {
        // The rate limit may have already been reset, so no need to wait
        if (DateTime.UtcNow >= _resetTime) 
            return;

        // Put our current monitor task into a waiting state
        // This will also block any other monitor tasks from proceeding until the initial task releases the semaphore
        await _semaphore.WaitAsync();
        try
        {
            // Figure out how long we need to wait for the rate limit to reset
            var delay = _resetTime - DateTime.UtcNow;

            // If there is still time remaining, we delay
            if (delay > TimeSpan.Zero) 
                await Task.Delay(delay);
        }
        finally
        {
            // Release the semaphore so other monitor tasks can proceed
            _semaphore.Release();
        }
    }
}