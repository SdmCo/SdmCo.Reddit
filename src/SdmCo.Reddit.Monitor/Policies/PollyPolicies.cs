using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;

namespace SdmCo.Reddit.Monitor.Policies;

public static class PollyPolicies
{
    // Retry policy for transient HTTP errors
    // Exponential backoff, 5 attempts max
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(5, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));

    // Circuit Breaker policy for transiet HTTP errors
    // After 5 exceptions, pause for 30 seconds befor reopening circuit
    public static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() => HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}