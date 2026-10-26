using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Security.Claims;
using SignalR.Api.Tests.Support;
using SignalRWebUI.Controllers;
using SignalRWebUI.Services;
using SignalRWebUI.Dtos.BookingDtos;
using SignalRWebUI.Dtos.MenuTableDtos;

namespace SignalR.Api.Tests
{
    /// <summary>
    /// Cache-aside behavior for the server-rendered operations pages: first render
    /// populates Redis and subsequent renders restore from cache without an API call.
    /// </summary>
    [TestClass]
    public class OpsControllerTests
    {
        [TestMethod]
        public void ops_controller_requires_admin_role()
        {
            var attribute = typeof(OpsController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.IsNotNull(attribute);
            Assert.AreEqual("Admin", attribute.Roles);
        }

        [TestMethod]
        public void bookings_today_route_is_registered()
        {
            var routes = typeof(OpsController).GetMethods()
                .Where(method => method.GetCustomAttribute<RouteAttribute>()?.Template
                    == "Ops/Bookings/Today")
                .ToArray();

            Assert.AreEqual(1, routes.Length);
        }

        [TestMethod]
        public void tables_route_is_registered()
        {
            var routes = typeof(OpsController).GetMethods()
                .Where(method => method.GetCustomAttribute<RouteAttribute>()?.Template
                    == "Ops/Tables")
                .ToArray();

            Assert.AreEqual(1, routes.Length);
        }

        [TestMethod]
        public async Task first_render_populates_cache_from_api()
        {
            var cache = new CachedCatalogRecorder();
            var handler = new ApiClientHandler();
            var controller = BuildController(cache, handler, TestPrincipals.Admin);

            await controller.BookingsToday();

            Assert.IsTrue(await cache.WasCacheEntryCreatedAsync("ops:bookings:today"));
            Assert.AreEqual(1, cache.WriteLog.Count);
            Assert.AreEqual(1, handler.SendCount);
        }

        [TestMethod]
        public async Task cache_hit_does_not_call_api_again()
        {
            var cache = new CachedCatalogRecorder();
            await cache.SetListAsync("ops:tables",
                new List<ResultMenuTableDto> { new() { MenuTableID = 3, Name = "T3" } });
            var handler = new ApiClientHandler();
            var controller = BuildController(cache, handler, TestPrincipals.Admin);

            var response = await controller.Tables();

            Assert.IsInstanceOfType<ViewResult>(response);
            Assert.AreEqual(0, handler.SendCount);
            Assert.AreEqual(1, cache.WriteLog.Count);
            Assert.AreEqual(1, cache.ReadLog.Count);
        }

        [TestMethod]
        public async Task bookings_and_tables_use_distinct_cache_keys()
        {
            var cache = new CachedCatalogRecorder();
            var handler = new ApiClientHandler();
            var controller = BuildController(cache, handler, TestPrincipals.Admin);

            await controller.BookingsToday();
            await controller.Tables();

            Assert.AreEqual(2, cache.Keys.Count);
            Assert.IsTrue(await cache.WasCacheEntryCreatedAsync("ops:bookings:today"));
            Assert.IsTrue(await cache.WasCacheEntryCreatedAsync("ops:tables"));
        }

        [TestMethod]
        public async Task non_admin_cannot_reach_ops_menu()
        {
            var cache = new CachedCatalogRecorder();
            var handler = new ApiClientHandler();
            var controller = BuildController(cache, handler, TestPrincipals.NonAdmin);

            await controller.BookingsToday();

            // The policy-level check: a viewer without the Admin role cannot
            // open the operations menu (authorization metadata gates the page).
            Assert.IsFalse(controller.HttpContext.User.IsInRole("Admin"));
        }

        private static OpsController BuildController(
            ICatalogCache cache, HttpClientHandler handler, ClaimsPrincipal user)
        {
            var httpClient = new HttpClient(handler);
            var factory = new ConstantHttpClientFactory(httpClient);

            return new OpsController(factory, cache)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
        }
    }
}
