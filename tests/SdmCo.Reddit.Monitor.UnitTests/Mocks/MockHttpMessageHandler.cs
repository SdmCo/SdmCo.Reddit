namespace SdmCo.Reddit.Monitor.UnitTests.Mocks;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responseGenerator;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseGenerator) =>
        _responseGenerator = responseGenerator ?? throw new ArgumentNullException(nameof(responseGenerator));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken) => Task.FromResult(_responseGenerator(request, cancellationToken));
}