using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SignalR.Api.Tests.Support;
using SignalRApi.Hubs;
using SignalRApi.Realtime;
using SignalR.BusinessLayer.Abstract;
using System.Net.Http.Json;

namespace SignalR.Api.Tests.Realtime
{
    /// <summary>
    /// Realtime delivery tests: the push publisher must target the operations group
    /// with "ReceiveOperationEvent", and subscriptions must join / leave the room.
    /// These mocks exercise the exact IHubContext contract SignalR would use live.
    /// </summary>
    [TestClass]
    public class RealtimeBroadcastTests
    {
        private static (Mock<IHubContext<OperationsHub>> HubContext,
            Mock<IClientProxy> GroupProxy) CreatePublisher(out SignalROperationsEventPublisher publisher)
        {
            var hubContext = new Mock<IHubContext<OperationsHub>>();
            var clients = new Mock<IHubClients>();
            var groupProxy = new Mock<IClientProxy>();

            clients.Setup(clients => clients.Group(OperationsHub.OperationsRoom))
                .Returns(groupProxy.Object);
            hubContext.SetupGet(hubContext => hubContext.Clients).Returns(clients.Object);

            publisher = new SignalROperationsEventPublisher(hubContext.Object);
            return (hubContext, groupProxy);
        }

        [TestMethod]
        public void operations_room_is_stable()
        {
            Assert.AreEqual("operations", OperationsHub.OperationsRoom);
        }

        [TestMethod]
        public async Task publish_sends_event_to_operations_group()
        {
            var (_, groupProxy) = CreatePublisher(out var publisher);
            var operationEvent = new OperationEvent("Booking", "12", "Create");

            await publisher.PublishAsync(OperationsHub.OperationsRoom, operationEvent);

            groupProxy.Verify(proxy => proxy.SendCoreAsync(
                "ReceiveOperationEvent",
                It.Is<object?[]>(args => args.Length == 1
                    && args[0] is OperationEvent
                    && ((OperationEvent)args[0]).EntityType == "Booking"
                    && ((OperationEvent)args[0]).EntityId == "12"
                    && ((OperationEvent)args[0]).Action == "Create"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task subscribe_joins_the_operations_group()
        {
            var hubContext = new Mock<IHubContext<OperationsHub>>();
            var groups = new Mock<IGroupManager>();
            hubContext.SetupGet(hubContext => hubContext.Groups).Returns(groups.Object);
            var publisher = new SignalROperationsEventPublisher(hubContext.Object);

            await publisher.SubscribeAsync("connection-1");

            groups.Verify(manager => manager.AddToGroupAsync(
                "connection-1", OperationsHub.OperationsRoom,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task unsubscribe_leaves_the_operations_group()
        {
            var hubContext = new Mock<IHubContext<OperationsHub>>();
            var groups = new Mock<IGroupManager>();
            hubContext.SetupGet(hubContext => hubContext.Groups).Returns(groups.Object);
            var publisher = new SignalROperationsEventPublisher(hubContext.Object);

            await publisher.UnsubscribeAsync("connection-2");

            groups.Verify(manager => manager.RemoveFromGroupAsync(
                "connection-2", OperationsHub.OperationsRoom,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task api_write_publishes_booking_created_event()
        {
            using TestApplication app = new();
            var payload = new
            {
                Name = "Realtime Guest",
                Email = "realtime@ops.test",
                Phone = "555-0102",
                PersonCount = 3,
                Date = DateTime.UtcNow.AddHours(2)
            };

            var response = await app.Client.PostAsJsonAsync("/api/Booking", payload);

            response.EnsureSuccessStatusCode();
            Assert.IsTrue(app.Recorder.Contains("Booking", "realtime@ops.test", "Create"));
        }
    }
}
