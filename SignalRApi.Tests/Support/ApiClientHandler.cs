using System.Net;
using System.Text;

namespace SignalR.Api.Tests.Support
{
    /// <summary>
    /// Test double for the API HttpClient. Sharing a private helper lets the
    /// Ops view controller tests reuse the same fake endpoint for cache reads.
    /// </summary>
    public class ApiClientHandler : HttpClientHandler
    {
        public int SendCount { get; private set; }

        public string LastUrl { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            LastUrl = request.RequestUri?.ToString() ?? string.Empty;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""[{"key":"test","value":1}]""",
                    Encoding.UTF8, "application/json")
            });
        }
    }
}
