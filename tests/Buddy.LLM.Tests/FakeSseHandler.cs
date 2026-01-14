using System.Net;
using System.Text;

namespace Buddy.LLM.Tests;

internal sealed class FakeSseHandler : HttpMessageHandler {
    private readonly string _sse;

    public FakeSseHandler(string sse) {
        _sse = sse;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(_sse)));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");

        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = content
        };

        return Task.FromResult(response);
    }
}
