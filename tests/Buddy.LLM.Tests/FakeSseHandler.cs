using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Buddy.LLM.Tests;

internal sealed class FakeSseHandler(string sse) : HttpMessageHandler {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(sse)));
        content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");

        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = content
        };

        return Task.FromResult(response);
    }
}
