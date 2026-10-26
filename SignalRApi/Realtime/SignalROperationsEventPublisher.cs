using Microsoft.AspNetCore.SignalR;
using SignalR.BusinessLayer.Abstract;
using SignalRApi.Hubs;

namespace SignalRApi.Realtime
{
    /// <summary>
    /// Realtime delivery surface used by both the API endpoints and the hub itself.
    /// Booking / payment / inventory mutations publish here; operators subscribe
    /// through <see cref="OperationsHub"/> and listen for "ReceiveOperationEvent".
    /// </summary>
    public interface IOperationsEventPublisher : IOperationEventBroadcaster
    {
        Task SubscribeAsync(string connectionId);

        Task UnsubscribeAsync(string connectionId);
    }

    public class SignalROperationsEventPublisher : IOperationsEventPublisher
    {
        private readonly IHubContext<OperationsHub> _hubContext;

        public SignalROperationsEventPublisher(IHubContext<OperationsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PublishAsync(string room, OperationEvent operationEvent)
        {
            await _hubContext.Clients.Group(room)
                .SendAsync("ReceiveOperationEvent", operationEvent);
        }

        public Task SubscribeAsync(string connectionId)
        {
            return _hubContext.Groups.AddToGroupAsync(
                connectionId, OperationsHub.OperationsRoom);
        }

        public Task UnsubscribeAsync(string connectionId)
        {
            return _hubContext.Groups.RemoveFromGroupAsync(
                connectionId, OperationsHub.OperationsRoom);
        }
    }
}
