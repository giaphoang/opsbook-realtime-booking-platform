using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SignalR.Api.Tests.Support;
using SignalR.BusinessLayer.Abstract;
using SignalR.BusinessLayer.ValidationRules.BookingRules;
using SignalR.DtoLayer.BookingDto;
using SignalR.EntityLayer.Entities;
using SignalRApi.Controllers;

namespace SignalR.Api.Tests.Controllers
{
    /// <summary>
    /// Controller-level unit tests: booking mutations must save through the service
    /// and publish a realtime event, validation failures must short-circuit both.
    /// </summary>
    [TestClass]
    public class BookingControllerDirectTests
    {
        [TestMethod]
        public async Task create_booking_broadcasts_push_event_to_operator_room()
        {
            var bookingService = new Mock<IBookingService>();
            var recorder = new OperationsEventRecorder();
            var controller = BuildController(bookingService.Object, recorder);

            var response = await controller.CreateBooking(new CreateBookingDto
            {
                Name = "Test User",
                Email = "test-user@example.com",
                Phone = "01-02-03",
                PersonCount = 2,
                Date = DateTime.UtcNow
            });

            Assert.IsInstanceOfType<OkObjectResult>(response);
            Assert.AreEqual(1, recorder.Events.Count);
            var (broadcastRoom, broadcastEvent) = recorder.Events[0];
            Assert.AreEqual("operations", broadcastRoom);
            Assert.AreEqual("Booking", broadcastEvent.EntityType);
            Assert.AreEqual("Create", broadcastEvent.Action);
            Assert.AreEqual("test-user@example.com", broadcastEvent.EntityId);
        }

        [TestMethod]
        public async Task create_booking_persists_through_booking_service()
        {
            var bookingService = new Mock<IBookingService>();
            var recorder = new OperationsEventRecorder();
            var controller = BuildController(bookingService.Object, recorder);

            await controller.CreateBooking(new CreateBookingDto
            {
                Name = "Persisted User",
                Email = "persisted@example.com",
                Phone = "01-02-03",
                PersonCount = 4,
                Date = DateTime.UtcNow
            });

            bookingService.Verify(service => service.TAdd(
                It.Is<Booking>(booking => booking.Email == "persisted@example.com")),
                Times.Once);
        }

        [TestMethod]
        public async Task create_booking_rejects_invalid_name_without_saving()
        {
            var bookingService = new Mock<IBookingService>();
            var recorder = new OperationsEventRecorder();
            var controller = BuildController(bookingService.Object, recorder);

            var response = await controller.CreateBooking(new CreateBookingDto
            {
                Name = "ab",
                Email = "invalid@example.com",
                Phone = "01-02-03",
                PersonCount = 2,
                Date = DateTime.UtcNow
            });

            Assert.IsInstanceOfType<BadRequestObjectResult>(response);
            bookingService.Verify(service => service.TAdd(It.IsAny<Booking>()), Times.Never);
            Assert.AreEqual(0, recorder.Events.Count);
        }

        [TestMethod]
        public async Task delete_booking_broadcasts_delete_event()
        {
            var bookingService = new Mock<IBookingService>();
            bookingService.Setup(service => service.TGetById(5)).Returns(new Booking
            {
                BookingID = 5,
                Name = "Cancelling User",
                Email = "cancel@example.com"
            });
            var recorder = new OperationsEventRecorder();
            var controller = BuildController(bookingService.Object, recorder);

            var response = await controller.DeleteBooking(5);

            Assert.IsInstanceOfType<OkObjectResult>(response);
            bookingService.Verify(service => service.TDelete(It.Is<Booking>(
                booking => booking.BookingID == 5)), Times.Once);
            Assert.IsTrue(recorder.Contains("Booking", "cancel@example.com", "Delete"));
        }

        [TestMethod]
        public async Task delete_missing_booking_returns_not_found()
        {
            var bookingService = new Mock<IBookingService>();
            bookingService.Setup(service => service.TGetById(999)).Returns((Booking?)null);
            var recorder = new OperationsEventRecorder();
            var controller = BuildController(bookingService.Object, recorder);

            var response = await controller.DeleteBooking(999);

            Assert.IsInstanceOfType<NotFoundResult>(response);
            bookingService.Verify(service => service.TDelete(It.IsAny<Booking>()), Times.Never);
            Assert.AreEqual(0, recorder.Events.Count);
        }

        [TestMethod]
        public async Task update_missing_booking_returns_not_found()
        {
            var bookingService = new Mock<IBookingService>();
            bookingService.Setup(service => service.TGetById(42)).Returns((Booking?)null);
            var recorder = new OperationsEventRecorder();
            var controller = BuildController(bookingService.Object, recorder);

            var response = await controller.UpdateBooking(new UpdateBookingDto
            {
                BookingID = 42,
                Name = "Nobody",
                Email = "nobody@example.com"
            });

            Assert.IsInstanceOfType<NotFoundResult>(response);
            bookingService.Verify(service => service.TUpdate(It.IsAny<Booking>()), Times.Never);
            Assert.AreEqual(0, recorder.Events.Count);
        }

        private static BookingController BuildController(
            IBookingService bookingService, OperationsEventRecorder recorder)
        {
            var mapperConfiguration = new MapperConfiguration(configuration =>
            {
                configuration.CreateMap<CreateBookingDto, Booking>();
                configuration.CreateMap<UpdateBookingDto, Booking>();
            });
            var mapper = mapperConfiguration.CreateMapper();
            return new BookingController(
                bookingService, mapper, new CreateBookingValidation(), recorder);
        }
    }
}
