using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalR.BusinessLayer.Abstract;
using SignalR.BusinessLayer.Concrete;
using SignalRApi.Hubs;
using SignalRApi.Performance;
using SignalRApi.Realtime;

namespace SignalRApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentWorkflowService _payments;
        private readonly IOperationEventBroadcaster _operationEventBroadcaster;

        public PaymentsController(IPaymentWorkflowService payments,
            IOperationEventBroadcaster operationEventBroadcaster)
        {
            _payments = payments;
            _operationEventBroadcaster = operationEventBroadcaster;
        }

        [Authorize(Policy = "Registrar")]
        [HttpPost]
        public async Task<IActionResult> Process(PaymentRequestDto request)
        {
            using var activity = OperationTelemetry.StartActivity("write.payment");

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return BadRequest(new { error = "IdempotencyKey is required." });
            }

            var result = await _payments.ProcessAsync(request);
            if (result is null)
            {
                return UnprocessableEntity(new { error = "Payment could not be processed." });
            }

            var operationEvent = new OperationEvent(
                "Payment", request.IdempotencyKey, result.Status.ToString());

            await _operationEventBroadcaster.PublishAsync(
                OperationsHub.OperationsRoom, operationEvent);

            return Ok(result);
        }
    }
}
