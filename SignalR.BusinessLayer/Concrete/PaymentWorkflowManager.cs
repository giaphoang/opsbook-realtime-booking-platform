using System.Collections.Concurrent;
using SignalR.BusinessLayer.Abstract;

namespace SignalR.BusinessLayer.Concrete
{
    public class PaymentRequestDto
    {
        public int BookingId { get; set; }

        public string IdempotencyKey { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
    }

    public class PaymentResultDto
    {
        public PaymentStatus Status { get; set; }

        public string? Reference { get; set; }

        public string Message { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }

    public enum PaymentMethod
    {
        Card = 0,
        Cash = 1,
        Invoice = 2
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Approved = 1,
        Declined = 2
    }

    public interface IPaymentWorkflowService
    {
        Task<PaymentResultDto?> ProcessAsync(PaymentRequestDto request);

        int ProcessedCount { get; }
    }

    /// <summary>
    /// Fake payment processor used by the booking endpoints. Duplicate work with
    /// the same idempotency key is answered from the processed map, so retries and
    /// duplicate callbacks never double-charge a booking.
    /// </summary>
    public class PaymentWorkflowManager : IPaymentWorkflowService
    {
        private readonly ConcurrentDictionary<string, PaymentResultDto> _processed =
            new(StringComparer.Ordinal);

        public int ProcessedCount => _processed.Count;

        public Task<PaymentResultDto?> ProcessAsync(PaymentRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                throw new ArgumentException(
                    "Payment requests require an idempotency key.", nameof(request));
            }

            if (request.Amount <= 0m)
            {
                return Task.FromResult<PaymentResultDto?>(new PaymentResultDto
                {
                    Status = PaymentStatus.Declined,
                    Reference = request.IdempotencyKey,
                    Amount = request.Amount,
                    Message = "Payment amount must be positive."
                });
            }

            return Task.FromResult<PaymentResultDto?>(_processed.GetOrAdd(
                request.IdempotencyKey,
                _ => new PaymentResultDto
                {
                    Status = PaymentStatus.Approved,
                    Reference = request.IdempotencyKey,
                    Amount = request.Amount,
                    Message = "Approved."
                }));
        }
    }
}
