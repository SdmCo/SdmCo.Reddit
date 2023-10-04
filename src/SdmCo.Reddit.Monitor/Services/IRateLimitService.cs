namespace SdmCo.Reddit.Monitor.Services;

public interface IRateLimitService
{
    void SetRateLimitInfo(int remaining, int resetInSeconds);
    bool CanMakeRequest();
    Task WaitForResetAsync();
}