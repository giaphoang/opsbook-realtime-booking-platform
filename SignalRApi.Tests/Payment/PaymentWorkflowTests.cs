using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SignalR.Api.Tests.Support;
using SignalR.BusinessLayer.Abstract;
using SignalR.BusinessLayer.Concrete;
using SignalRApi.Controllers;

namespace SignalR.Api.Tests.Payment
{
    /// <summary>
    /// Payment workflow tests: approval, declines, and idempotency guarantees for
    /// retries and duplicate gateway callbacks.
    /// </summary>
    [TestClass]
    public class PaymentWorkflowTests
    {
        [TestMethod]
        public async Task approves_positive_amount()
        {
            var payments = new PaymentWorkflowManager();

            var result = await payments.ProcessAsync(new PaymentRequestDto
            {
                BookingId = 1, IdempotencyKey = "pay-01", Amount = 120m
            });

            Assert.IsNotNull(result);
            Assert.AreEqual(PaymentStatus.Approved, result.Status);
            Assert.AreEqual("pay-01", result.Reference);
            Assert.AreEqual(120m, result.Amount);
        }

        [TestMethod]
        public async Task declines_zero_amount()
        {
            var payments = new PaymentWorkflowManager();

            var result = await payments.ProcessAsync(new PaymentRequestDto
            {
                BookingId = 1, IdempotencyKey = "pay-02", Amount = 0m
            });

            Assert.IsNotNull(result);
            Assert.AreEqual(PaymentStatus.Declined, result.Status);
        }

        [TestMethod]
        public async Task declines_negative_amount()
        {
            var payments = new PaymentWorkflowManager();

            var result = await payments.ProcessAsync(new PaymentRequestDto
            {
                BookingId = 1, IdempotencyKey = "pay-03", Amount = -5m
            });

            Assert.IsNotNull(result);
            Assert.AreEqual(PaymentStatus.Declined, result.Status);
        }

        [TestMethod]
        public async Task declined_request_is_not_recorded_as_processed()
        {
            var payments = new PaymentWorkflowManager();

            await payments.ProcessAsync(new PaymentRequestDto
            {
                BookingId = 1, IdempotencyKey = "pay-04", Amount = 0m
            });

            Assert.AreEqual(0, payments.ProcessedCount);
        }

        [TestMethod]
        public async Task duplicate_request_with_same_key_returns_original_result()
        {
            var payments = new PaymentWorkflowManager();
            var request = new PaymentRequestDto
            {
                BookingId = 7, IdempotencyKey = "dup-key", Amount = 55m
            };

            var first = await payments.ProcessAsync(request);
            var second = await payments.ProcessAsync(request);

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.AreEqual(first.Status, second.Status);
            Assert.AreEqual(first.Reference, second.Reference);
            Assert.AreEqual(1, payments.ProcessedCount);
        }

        [TestMethod]
        public async Task duplicate_callback_is_processed_exactly_once()
        {
            var payments = new PaymentWorkflowManager();

            // Simulates the gateway retrying the same callback twice.
            await payments.ProcessAsync(new PaymentRequestDto
                { BookingId = 7, IdempotencyKey = "callback-1", Amount = 10m });
            await payments.ProcessAsync(new PaymentRequestDto
                { BookingId = 7, IdempotencyKey = "callback-1", Amount = 10m });
            await payments.ProcessAsync(new PaymentRequestDto
                { BookingId = 7, IdempotencyKey = "callback-1", Amount = 10m });

            Assert.AreEqual(1, payments.ProcessedCount);
        }

        [TestMethod]
        public async Task distinct_keys_are_processed_independently()
        {
            var payments = new PaymentWorkflowManager();

            await payments.ProcessAsync(new PaymentRequestDto
                { BookingId = 1, IdempotencyKey = "a", Amount = 1m });
            await payments.ProcessAsync(new PaymentRequestDto
                { BookingId = 2, IdempotencyKey = "b", Amount = 2m });

            Assert.AreEqual(2, payments.ProcessedCount);
        }

        [TestMethod]
        public async Task missing_idempotency_key_throws()
        {
            var payments = new PaymentWorkflowManager();

            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                payments.ProcessAsync(new PaymentRequestDto
                    { BookingId = 1, IdempotencyKey = "", Amount = 10m }));
        }

        [TestMethod]
        public async Task telemetry_decorator_delegates_payment_result()
        {
            var inner = new PaymentWorkflowManager();
            var telemetry = new PaymentWorkflowTelemetry(inner);

            var result = await telemetry.ProcessAsync(new PaymentRequestDto
                { BookingId = 1, IdempotencyKey = "tel-1", Amount = 20m });

            Assert.IsNotNull(result);
            Assert.AreEqual(PaymentStatus.Approved, result.Status);
            Assert.AreEqual(1, telemetry.ProcessedCount);
        }

        [TestMethod]
        public async Task payments_controller_rejects_missing_idempotency_key()
        {
            var recorder = new OperationsEventRecorder();
            var controller = new PaymentsController(
                new PaymentWorkflowManager(), recorder);

            var response = await controller.Process(new PaymentRequestDto
                { BookingId = 1, IdempotencyKey = " ", Amount = 10m });

            Assert.IsInstanceOfType<BadRequestObjectResult>(response);
            Assert.AreEqual(0, recorder.Events.Count);
        }

        [TestMethod]
        public async Task payments_controller_publishes_status_event()
        {
            var recorder = new OperationsEventRecorder();
            var controller = new PaymentsController(
                new PaymentWorkflowManager(), recorder);

            var response = await controller.Process(new PaymentRequestDto
                { BookingId = 1, IdempotencyKey = "publish-1", Amount = 30m });

            Assert.IsInstanceOfType<OkObjectResult>(response);
            Assert.IsTrue(recorder.Contains("Payment", "publish-1", "Approved"));
        }
    }
}
