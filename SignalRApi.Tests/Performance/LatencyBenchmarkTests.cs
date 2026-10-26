using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignalR.BusinessLayer.Concrete;

namespace SignalR.Api.Tests.Performance
{
    /// <summary>
    /// Delivery-latency benchmark tests: polling vs SignalR push measured on the
    /// same write workload, plus input validation and percentile sanity checks.
    /// </summary>
    [TestClass]
    public class LatencyBenchmarkTests
    {
        [TestMethod]
        public async Task push_p95_is_microsecond_scale_not_poll_scale()
        {
            var service = new InMemoryRealtimeLatencyBenchmarkService();

            var result = await service.BenchmarkAsync(new LatencyBenchmarkRequest
            {
                Mode = DeliveryMode.SignalRPush,
                Iterations = 24,
                PollingIntervalMs = 0
            });

            Assert.IsTrue(result.P95Ms < 10,
                $"Push P95 should stay under 10ms, measured {result.P95Ms:F2}ms");
        }

        [TestMethod]
        public async Task polling_p95_tracks_the_poll_interval()
        {
            var service = new InMemoryRealtimeLatencyBenchmarkService();

            var result = await service.BenchmarkAsync(new LatencyBenchmarkRequest
            {
                Mode = DeliveryMode.DatabasePolling,
                Iterations = 24,
                PollingIntervalMs = 30
            });

            Assert.IsTrue(result.P95Ms >= 27,
                $"Polling P95 should be near the 30ms interval, measured {result.P95Ms:F2}ms");
        }

        [TestMethod]
        public async Task push_is_far_faster_than_polling_under_same_workload()
        {
            var service = new InMemoryRealtimeLatencyBenchmarkService();

            var push = await service.BenchmarkAsync(new LatencyBenchmarkRequest
            {
                Mode = DeliveryMode.SignalRPush,
                Iterations = 24,
                PollingIntervalMs = 0
            });
            var polling = await service.BenchmarkAsync(new LatencyBenchmarkRequest
            {
                Mode = DeliveryMode.DatabasePolling,
                Iterations = 24,
                PollingIntervalMs = 30
            });

            Assert.IsTrue(push.MeanMs < polling.MeanMs / 2,
                $"Push mean {push.MeanMs:F2}ms should be well below polled {polling.MeanMs:F2}ms");
        }

        [TestMethod]
        public async Task percentiles_are_ordered_correctly()
        {
            var service = new InMemoryRealtimeLatencyBenchmarkService();

            var result = await service.BenchmarkAsync(new LatencyBenchmarkRequest
            {
                Mode = DeliveryMode.DatabasePolling,
                Iterations = 32,
                PollingIntervalMs = 20
            });

            Assert.IsTrue(result.MinMs <= result.P50Ms);
            Assert.IsTrue(result.P50Ms <= result.P95Ms);
            Assert.IsTrue(result.P95Ms <= result.P99Ms);
            Assert.IsTrue(result.P99Ms <= result.MaxMs);
        }

        [TestMethod]
        public async Task rejects_zero_iterations()
        {
            var service = new InMemoryRealtimeLatencyBenchmarkService();

            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                service.BenchmarkAsync(new LatencyBenchmarkRequest
                {
                    Mode = DeliveryMode.SignalRPush,
                    Iterations = 0,
                    PollingIntervalMs = 0
                }));
        }

        [TestMethod]
        public async Task rejects_invalid_polling_interval()
        {
            var service = new InMemoryRealtimeLatencyBenchmarkService();

            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                service.BenchmarkAsync(new LatencyBenchmarkRequest
                {
                    Mode = DeliveryMode.DatabasePolling,
                    Iterations = 4,
                    PollingIntervalMs = 0
                }));
        }
    }
}
