using System.Net;
using System.Text;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignalR.Api.Tests.Support;

namespace SignalR.Api.Tests
{
    /// <summary>
    /// End-to-end WebAPI controller tests over the full HTTP pipeline.
    /// </summary>
    [TestClass]
    public class BookingApiControllerTests
    {
        [TestMethod]
        public async Task get_bookings_without_token_returns_unauthorized()
        {
            using TestApplication app = new();
            app.Client.DefaultRequestHeaders.Authorization = null;

            var response = await app.Client.GetAsync("/api/Booking");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task get_bookings_with_token_returns_seeded_booking()
        {
            using TestApplication app = new();

            var response = await app.Client.GetAsync("/api/Booking");
            var body = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(body.Contains("direct@example.com"), $"Unexpected body: {body}");
        }

        [TestMethod]
        public async Task create_booking_with_invalid_body_returns_validation_errors()
        {
            using TestApplication app = new();
            var payload = new { Name = "ab", Email = "", Phone = "", PersonCount = 0, Date = DateTime.UtcNow };

            var response = await app.Client.PostAsJsonAsync("/api/Booking", payload);
            var body = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsTrue(body.Contains("Name"), $"Unexpected body: {body}");
        }

        [TestMethod]
        public async Task create_booking_valid_body_persists_and_publishes_event()
        {
            using TestApplication app = new();
            var payload = new
            {
                Name = "Ops Test Guest",
                Email = "guest@ops.test",
                Phone = "555-0101",
                PersonCount = 2,
                Date = DateTime.UtcNow.AddHours(4)
            };

            var response = await app.Client.PostAsJsonAsync("/api/Booking", payload);
            var body = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(body.Contains("Create"), $"Unexpected body: {body}");
            Assert.IsTrue(app.Recorder.Contains("Booking", "guest@ops.test", "Create"),
                "Booking creation should publish a realtime event to the operations room.");
        }

        [TestMethod]
        public async Task delete_unknown_booking_returns_not_found()
        {
            using TestApplication app = new();

            var response = await app.Client.DeleteAsync("/api/Booking/999999");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
