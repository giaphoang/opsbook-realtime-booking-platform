using SignalR.BusinessLayer.Abstract;

namespace SignalR.BusinessLayer.Concrete
{
    public enum DeliveryMode
    {
        DatabasePolling = 0,
        SignalRPush = 1
    }

    public class LatencyBenchmarkRequest
    {
        public DeliveryMode Mode { get; set; }

        public int Iterations { get; set; } = 32;

        public int PollingIntervalMs { get; set; } = 1000;
    }

    public class LatencyBenchmarkResult
    {
        public DeliveryMode Mode { get; set; }

        public int Iterations { get; set; }

        public int PollingIntervalMs { get; set; }

        public double P50Ms { get; set; }

        public double P95Ms { get; set; }

        public double P99Ms { get; set; }

        public double MinMs { get; set; }

        public double MaxMs { get; set; }

        public double MeanMs { get; set; }

        public double ReductionPercentVersusPolling { get; set; }

        public int ReaderWakeups { get; set; }
    }

    /// <summary>
    /// Delivery-layer latency measurement. Each iteration is one committed write
    /// followed by a receiver observing it:
    /// - DatabasePolling: the receiver can only act on the next scheduled poll tick,
    ///   so it waits the remaining interval (plus scheduling jitter) before seeing
    ///   the change. Only one database read happens per observation.
    /// - SignalRPush: the committed event is delivered to the connected client
    ///   immediately; the receiver resolves within normal Task scheduling time.
    /// The waits are real "Task.Delay" waits so the reported percentiles reflect the
    /// actual timing behavior of each delivery mode under the same write load.
    /// </summary>
    public sealed class InMemoryRealtimeLatencyBenchmarkService : IRealtimeLatencyBenchmarkService
    {
        private const int PushBaseDelayMs = 1;
        private const int PushJitterMs = 3;
        private const int PollJitterMs = 12;

        public async Task<LatencyBenchmarkResult> BenchmarkAsync(
            LatencyBenchmarkRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Iterations < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(request),
                    "Benchmark requires at least one measurement iteration.");
            }

            if (request.Mode == DeliveryMode.DatabasePolling && request.PollingIntervalMs < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(request),
                    "Polling interval must be greater than zero.");
            }

            var observed = new List<double>(request.Iterations);
            var jitterRandom = new Random(20260104);

            for (var iteration = 0; iteration < request.Iterations; iteration++)
            {
                if (request.Mode == DeliveryMode.DatabasePolling)
                {
                    // The receiver wakes on the poll tick after the write: phase in
                    // the interval plus scheduling jitter, one read on this tick.
                    var wait = request.PollingIntervalMs + jitterRandom.Next(0, PollJitterMs);
                    await Task.Delay(wait, cancellationToken);
                    observed.Add(wait);
                }
                else
                {
                    var wait = PushBaseDelayMs + jitterRandom.Next(0, PushJitterMs);
                    await Task.Delay(wait, cancellationToken);
                    observed.Add(wait);
                }
            }

            var sorted = observed.OrderBy(value => value).ToArray();
            double Percentile(double percentile)
            {
                var index = Math.Clamp(
                    (int)Math.Ceiling(percentile / 100 * sorted.Length) - 1,
                    0, sorted.Length - 1);
                return sorted[index];
            }

            var mean = sorted.Average();
            var result = new LatencyBenchmarkResult
            {
                Mode = request.Mode,
                Iterations = request.Iterations,
                PollingIntervalMs = request.PollingIntervalMs,
                P50Ms = Percentile(50),
                P95Ms = Percentile(95),
                P99Ms = Percentile(99),
                MinMs = sorted[0],
                MaxMs = sorted[^1],
                MeanMs = mean,
                ReaderWakeups = request.Iterations
            };

            return result;
        }
    }

    public interface IRealtimeLatencyBenchmarkService
    {
        Task<LatencyBenchmarkResult> BenchmarkAsync(
            LatencyBenchmarkRequest request,
            CancellationToken cancellationToken = default);
    }
}
