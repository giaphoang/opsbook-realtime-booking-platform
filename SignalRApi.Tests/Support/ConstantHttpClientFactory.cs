using System.Net.Http;

namespace SignalR.Api.Tests.Support
{
    public class ConstantHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public ConstantHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }
}
