using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalR.BusinessLayer.Abstract;
using SignalR.DataAccessLayer.Abstract;
using SignalR.DataAccessLayer.Concrete;
using SignalRApi.Hubs;
using SignalRApi.Realtime;

namespace SignalR.Api.Tests.Support
{
    /// <summary>
    /// Boots SignalRApi without SQL Server. Realtime delivery is recorded instead of
    /// broadcast (events still flow through the same <see cref="IOperationEventBroadcaster"/>
    /// contract), and EF Core reads/writes go to an in-memory database.
    /// </summary>
    public sealed class TestApplication : IDisposable
    {
        public TestApplication()
        {
            Recorder = new OperationsEventRecorder();

            App = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "InMemory",
                        ["Ops:UseInMemoryDatabase"] = "true",
                        ["Registrar:SigningKey"] = JwtTokenFactory.DefaultSigningKey
                    });
                });

                builder.ConfigureServices(services =>
                {
                    foreach (var descriptor in services.Where(descriptor =>
                        descriptor.ServiceType == typeof(DbContextOptions<SignalRContext>)
                        || descriptor.ServiceType == typeof(IOperationsEventPublisher)
                        || descriptor.ServiceType == typeof(IOperationEventBroadcaster)).ToList())
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<SignalRContext>(options =>
                        options.UseInMemoryDatabase($"realtime-ops-{Guid.NewGuid():N}"));
                    services.AddSingleton<IOperationEventBroadcaster>(Recorder);
                    services.AddSingleton<IOperationsEventPublisher>(Recorder);
                    services.AddSingleton<IBookingDal, InMemoryBookingDal>();
                });
            });

            Client = App.CreateDefaultClient();
            Client.DefaultRequestHeaders.Authorization =
                new("Bearer", JwtTokenFactory.CreateToken("550e8400-e29b-41d4-a716-446655440000"));
        }

        public WebApplicationFactory<Program> App { get; }

        public HttpClient Client { get; }

        public OperationsEventRecorder Recorder { get; }

        public IConfiguration Configuration => App.Services.GetRequiredService<IConfiguration>();

        public void Dispose()
        {
            Client.Dispose();
            App.Dispose();
        }
    }
}
