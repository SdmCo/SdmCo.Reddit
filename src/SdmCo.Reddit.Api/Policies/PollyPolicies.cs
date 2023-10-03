using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;

namespace SdmCo.Reddit.Api.Policies;

public static class PollyPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(5, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));

    public static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() => HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}