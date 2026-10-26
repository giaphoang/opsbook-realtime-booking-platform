using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using SignalR.BusinessLayer.Abstract;

namespace SignalR.BusinessLayer.Concrete
{
    /// <summary>
    /// Adds payment telemetry without replacing payment authorization rules.
    /// </summary>
    public class PaymentWorkflowTelemetry : IPaymentWorkflowService
    {
        private readonly IPaymentWorkflowService _payments;
        private static readonly Meter Meter = new("Payments.Workflow", "1.0.0");

        public PaymentWorkflowTelemetry(IPaymentWorkflowService payments)
        {
            _payments = payments;
        }

        public async Task<PaymentResultDto?> ProcessAsync(PaymentRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await _payments.ProcessAsync(request);
            stopwatch.Stop();

            var descriptor = Meter.CreateCounter<long>("write_latency_ms");
            descriptor.Add(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>(
                "component", nameof(PaymentWorkflowTelemetry)));
            return result;
        }

        public int ProcessedCount => _payments.ProcessedCount;
    }
}
