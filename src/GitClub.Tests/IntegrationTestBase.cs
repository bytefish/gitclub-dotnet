// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Constants;
using GitClub.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenFga.Sdk.Client;
using System.Security.Claims;

namespace GitClub.Tests
{
    /// <summary>
    /// Will be used by all integration tests, that need an <see cref="ApplicationDbContext"/>.
    /// </summary>
    public class IntegrationTestBase
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        protected IConfiguration Configuration { get; set; } = null!;

        /// <summary>
        /// Service Provider.
        /// </summary>
        protected ServiceProvider ServiceProvider { get; set; } = null!;

        public IntegrationTestBase()
        {
            CreateConfiguration();
            CreateServiceProvider();
        }

        /// <summary>
        /// Read the appsettings.json for the Test.
        /// </summary>
        /// <returns></returns>
        private void CreateConfiguration()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }

        /// <summary>
        /// Creates the ServiceProvider for the Tests.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void CreateServiceProvider()
        {
            var services = new ServiceCollection();

            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var connectionString = Configuration.GetConnectionString("ApplicationDatabase");

                if (connectionString == null)
                {
                    throw new InvalidOperationException("No ConnectionString named 'ApplicationDatabase' was found");
                }

                // Since version 7.0, NpgsqlDataSource is the recommended way to use Npgsql. When using NpsgqlDataSource,
                // NodaTime currently has to be configured twice - once at the EF level, and once at the underlying ADO.NET
                // level (there are plans to improve this):
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

                // Call UseNodaTime() when building your data source:
                dataSourceBuilder.UseNodaTime();

                var dataSource = dataSourceBuilder.Build();

                // Then, when configuring EF Core with UseNpgsql(), call UseNodaTime() there as well:
                options
                    .EnableSensitiveDataLogging()
                    .UseNpgsql(dataSource, options => options.UseNodaTime());
            });

            // Services
            services.AddScoped<UserService>();
            services.AddScoped<OrganizationService>();
            services.AddScoped<TeamService>();
            services.AddScoped<IssueService>();

            // OpenFGA
            services.AddSingleton<OpenFgaClient>(sp =>
            {
                var clientConfiguration = new ClientConfiguration
                {
                    ApiUrl = Configuration.GetValue<string>("OpenFGA:ApiUrl")!,
                    StoreId = Configuration.GetValue<string>("OpenFGA:StoreId")!,
                    AuthorizationModelId = Configuration.GetValue<string>("OpenFGA:AuthorizationModelId")!,
                };

                return new OpenFgaClient(clientConfiguration);
            });

            services.AddScoped<AclService>();

            ServiceProvider = services.BuildServiceProvider();
        }

        [TestInitialize]
        protected virtual async Task SetupAsync()
        {
            var claims = await ServiceProvider.GetRequiredService<UserService>().GetClaimsAsync(
                email: "philipp@bytefish.de",
                roles: [ Roles.Administrator, Roles.User ],
                cancellationToken: default);

            var user = await ServiceProvider.GetRequiredService<UserService>().GetUserByEmailAsync(
                email: "philipp@bytefish.de",
                cancellationToken: default);

            CurrentUser = new CurrentUser
            {
                Principal = new ClaimsPrincipal(new ClaimsIdentity(claims)),
                User =  user
            };
        }

        /// <summary>
        /// Gets or sets the Current User.
        /// </summary>
        protected CurrentUser CurrentUser { get; set; } = null!;
    }
}