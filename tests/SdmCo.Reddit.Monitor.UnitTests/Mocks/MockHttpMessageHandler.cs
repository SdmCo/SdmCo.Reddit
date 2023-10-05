namespace SdmCo.Reddit.Monitor.UnitTests.Mocks;

public class MockHttpMessageHandler : HttpMessageHandler
{
    // Function to represent code to generate a custom HttpResponseMessage
    private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responseGenerator;

    // Create a function to generate an HttpResonseMessage based on the HttpRequestMessage passed in
    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseGenerator) =>
        _responseGenerator = responseGenerator ?? throw new ArgumentNullException(nameof(responseGenerator));

    // Overrides HttpClient SendAsync to return a pre-defined HttpResponseMessage
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken) => Task.FromResult(_responseGenerator(request, cancellationToken));
}