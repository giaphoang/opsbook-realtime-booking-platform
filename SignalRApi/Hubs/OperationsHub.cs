using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalR.BusinessLayer.Abstract;
using SignalRApi.Realtime;

namespace SignalRApi.Hubs
{
    /// <summary>
    /// Realtime operator channel. Clients join once with SubscribeToOperations and
    /// then receive committed booking / payment events via "ReceiveOperationEvent"
    /// instead of polling the database for changes.
    /// </summary>
    [Authorize(Policy = "Registrar")]
    public class OperationsHub : Hub
    {
        public const string OperationsRoom = "operations";

        private readonly IOperationsEventPublisher _publisher;

        public OperationsHub(IOperationsEventPublisher publisher)
        {
            _publisher = publisher;
        }

        public Task SubscribeToOperations()
        {
            return _publisher.SubscribeAsync(Context.ConnectionId);
        }

        public Task SendOperationEvent(string entityType, string entityId, string action)
        {
            return _publisher.PublishAsync(
                OperationsRoom, new OperationEvent(entityType, entityId, action));
        }
    }
}
