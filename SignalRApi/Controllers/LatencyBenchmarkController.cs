using Microsoft.AspNetCore.Mvc;
using SignalR.BusinessLayer.Abstract;
using SignalR.BusinessLayer.Concrete;

namespace SignalRApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LatencyBenchmarkController : ControllerBase
    {
        private readonly IRealtimeLatencyBenchmarkService _benchmarkService;

        public LatencyBenchmarkController(IRealtimeLatencyBenchmarkService benchmarkService)
        {
            _benchmarkService = benchmarkService;
        }

        [HttpPost]
        public async Task<IActionResult> Benchmark(
            [FromBody] LatencyBenchmarkRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await _benchmarkService.BenchmarkAsync(request, cancellationToken);
            return Ok(result);
        }
    }
}
