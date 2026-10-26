namespace SignalR.BusinessLayer.Abstract
{
    public class OperationEvent
    {
        public OperationEvent()
        {
        }

        public OperationEvent(string entityType, string entityId, string action)
            : this(entityType, entityId, action, DateTimeOffset.UtcNow)
        {
        }

        public OperationEvent(string entityType, string entityId, string action, DateTimeOffset publishedAt)
        {
            EntityType = entityType;
            EntityId = entityId;
            Action = action;
            PublishedAt = publishedAt;
        }

        public string EntityType { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public DateTimeOffset PublishedAt { get; set; }
    }

    public interface IOperationEventBroadcaster
    {
        Task PublishAsync(string room, OperationEvent operationEvent);
    }
}
