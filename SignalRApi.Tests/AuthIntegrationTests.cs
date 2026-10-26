using System.Net;
using System.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignalR.Api.Tests.Support;

namespace SignalR.Api.Tests
{
    /// <summary>
    /// Exercises the JWT bearer authentication middleware end-to-end against the
    /// real application pipeline (WebApplicationFactory, no mocks of the API).
    /// </summary>
    [TestClass]
    public class AuthIntegrationTests
    {
        [TestMethod]
        public async Task request_without_token_returns_unauthorized()
        {
            using TestApplication app = new();
            app.Client.DefaultRequestHeaders.Authorization = null;

            var response = await app.Client.GetAsync("/api/Booking");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task valid_bearer_token_is_accepted()
        {
            using TestApplication app = new();
            app.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", JwtTokenFactory.CreateToken("supported-employee"));

            var response = await app.Client.GetAsync("/api/Booking");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task basic_auth_scheme_is_rejected()
        {
            using TestApplication app = new();
            app.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String("user:password"u8.ToArray()));

            var response = await app.Client.GetAsync("/api/Booking");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task malformed_bearer_token_is_rejected()
        {
            using TestApplication app = new();
            app.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", "short-token");

            var response = await app.Client.GetAsync("/api/Booking");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task expired_bearer_token_is_rejected()
        {
            using TestApplication app = new();
            app.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", JwtTokenFactory.CreateToken(
                    "expired-token", expires: DateTime.UtcNow.AddMinutes(-10)));

            var response = await app.Client.GetAsync("/api/Booking");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task token_signed_with_wrong_key_is_rejected()
        {
            using TestApplication app = new();
            app.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", JwtTokenFactory.CreateToken(
                    "forged-token", signingKey: "not-the-configured-signing-key-value!!"));

            var response = await app.Client.GetAsync("/api/Booking");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task anonymous_hub_negotiate_is_rejected()
        {
            using TestApplication app = new();
            app.Client.DefaultRequestHeaders.Authorization = null;

            var response = await app.Client.GetAsync(
                "/OperationsHub/negotiate?negotiateVersion=1");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
