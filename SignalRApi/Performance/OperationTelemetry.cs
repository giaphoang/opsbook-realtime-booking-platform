using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SignalRApi.Performance
{
    /// <summary>
    /// Small performance surface shared by tests and endpoints. It records write
    /// latency/soft-failures and tags the delivery transport with the measurement.
    /// </summary>
    public static class OperationTelemetry
    {
        private static readonly Meter Meter = new("RealTimeOps", "1.0");

        public static readonly Counter<long> WriteLatencyMs = Meter
            .CreateCounter<long>("write_latency_ms");

        public static readonly Counter<long> DeliveryLatencyMs = Meter
            .CreateCounter<long>("delivery_latency_ms");

        public static readonly Counter<long> DeliveryFailures = Meter
            .CreateCounter<long>("delivery_failures");

        [Conditional("DEBUG")]
        private static void DebugTrace(Activity? activity)
        {
            if (activity is null)
            {
                return;
            }

            Console.WriteLine(activity.DisplayName);
        }

        public static Activity? StartActivity(string name)
        {
            var activity = Activity.Current?.Source.StartActivity(name);
            DebugTrace(activity);
            return activity;
        }
    }
}
