using Microsoft.EntityFrameworkCore;
using SignalR.DataAccessLayer.Abstract;
using SignalR.DataAccessLayer.Concrete;
using SignalR.DataAccessLayer.Repositories;
using SignalR.EntityLayer.Entities;

namespace SignalR.Api.Tests.Support
{
    /// <summary>
    /// In-memory booking repository backing the API integration tests.
    /// </summary>
    public class InMemoryBookingDal : GenericRepository<Booking>, IBookingDal
    {
        private readonly string _databaseName;

        public InMemoryBookingDal()
            : base(CreateContext(out var databaseName))
        {
            _databaseName = databaseName;
        }

        private static SignalRContext CreateContext(out string databaseName)
        {
            databaseName = $"booking-tests-{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<SignalRContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            var context = new SignalRContext(options);
            context.Bookings.Add(new Booking()
            {
                BookingID = 9,
                Name = "Direct Participant",
                Email = "direct@example.com",
                Phone = "555-0100",
                Date = DateTime.UtcNow,
                PersonCount = 2
            });
            context.SaveChanges();
            return context;
        }
    }
}
