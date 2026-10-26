using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SignalRWebUI.Dtos.BookingDtos;
using SignalRWebUI.Dtos.MenuTableDtos;
using SignalRWebUI.Services;

namespace SignalRWebUI.Controllers
{
    /// <summary>
    /// Server-rendered operations views. The model is restored from Redis when a
    /// read-heavy render is safe; pages are still dynamic MVC/Razor responses.
    /// </summary>
    [Authorize(Roles ="Admin")]
    public class OpsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICatalogCache _catalogCache;

        public OpsController(IHttpClientFactory httpClientFactory, ICatalogCache catalogCache)
        {
            _httpClientFactory = httpClientFactory;
            _catalogCache = catalogCache;
        }

        [Route("Ops/Bookings/Today")]
        public Task<IActionResult> BookingsToday()
        {
            return RenderCachedListAsync<ResultBookingDto>("ops:bookings:today",
                "https://localhost:7171/api/Booking", "/Ops/Bookings/Today");
        }

        [Route("Ops/Tables")]
        public Task<IActionResult> Tables()
        {
            return RenderCachedListAsync<ResultMenuTableDto>("ops:tables",
                "https://localhost:7171/api/MenuTables", "/Ops/Tables");
        }

        private async Task<IActionResult> RenderCachedListAsync<T>(
            string cacheKey, string apiEndpoint, string routeName) where T : class
        {
            var cached = await _catalogCache.GetListAsync<List<T>>(cacheKey);
            if (cached is not null)
            {
                return View("Index", cached);
            }

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(apiEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                return View("Index", new List<T>());
            }
            var values = JsonConvert.DeserializeObject<List<T>>(await response.Content.ReadAsStringAsync())
                ?? new List<T>();

            await _catalogCache.SetListAsync(cacheKey, values, TimeSpan.FromHours(1));
            return View("Index", values);
        }
    }
}
