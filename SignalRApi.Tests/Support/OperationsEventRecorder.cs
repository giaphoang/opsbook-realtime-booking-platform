using SignalR.BusinessLayer.Abstract;
using SignalRApi.Hubs;

namespace SignalR.Api.Tests.Support
{
    /// <summary>
    /// Records every realtime event the application publishes. In the TestApplication
    /// it replaces the SignalR-backed publisher so tests can assert what operators
    /// would receive over the /OperationsHub connection.
    /// </summary>
    public sealed class OperationsEventRecorder : SignalRApi.Realtime.IOperationsEventPublisher
    {
        private readonly List<(string Room, OperationEvent Event)> _events = new();

        public IReadOnlyList<(string Room, OperationEvent Event)> Events => _events;

        public Task PublishAsync(string room, OperationEvent operationEvent)
        {
            _events.Add((room, operationEvent));
            return Task.CompletedTask;
        }

        public void Reset() => _events.Clear();

        public Task SubscribeAsync(string connectionId) => Task.CompletedTask;

        public Task UnsubscribeAsync(string connectionId) => Task.CompletedTask;

        public bool Contains(string entityType, string entityId, string action)
        {
            return _events.Any(broadcast => broadcast.Room == OperationsHub.OperationsRoom
                && broadcast.Event.EntityType == entityType
                && broadcast.Event.EntityId == entityId
                && broadcast.Event.Action == action);
        }
    }
}
