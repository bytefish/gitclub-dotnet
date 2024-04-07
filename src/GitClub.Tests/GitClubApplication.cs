using GitClub.Hosted;
using GitClub.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace GitClub.Tests
{
    internal class GitClubApplication : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(x =>
            {
                x.AddJsonFile("appsettings.json");
            });

            builder.ConfigureServices(services =>
            {
            services.AddLogging();

                // Remove the Postgres Outbox Event Processor, because it is 
                // hard to test with all the timing dependencies.
                {
                    var descriptor = services.First(x => x.ImplementationType == typeof(PostgresOutboxEventProcessor));

                    services.Remove(descriptor);
                }

                // Add a OutboxEventProcessor we can trigger manually.
                services.AddSingleton<OutboxEventProcessor>();
            });

            return base.CreateHost(builder);
        }
    }
}