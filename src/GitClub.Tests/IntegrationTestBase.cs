// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Infrastructure;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Constants;
using GitClub.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;
using System.Security.Claims;
using System.Threading;

namespace GitClub.Tests
{
    /// <summary>
    /// Will be used by all integration tests, that need an <see cref="ApplicationDbContext"/>. The Scope is "Per Test", 
    /// so you get a "fresh" set of services registered as "Scoped" for each and every new test. It's questionable, if 
    /// this holds for every test.
    /// </summary>
    public abstract class IntegrationTestBase
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        protected IConfiguration Configuration { get; set; } = null!;

        /// <summary>
        /// Service Provider.
        /// </summary>
        protected ServiceProvider ServiceProvider { get; set; } = null!;

        /// <summary>
        /// Service Scope.
        /// </summary>
        protected IServiceScope ServiceScope { get; set; } = null!;

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

            // Logging
            services.AddLogging();

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
            services.AddScoped<RepositoryService>();
            services.AddScoped<IssueService>();

            // OpenFGA
            services.AddSingleton<OpenFgaClient>(sp =>
            {
                var clientConfiguration = new ClientConfiguration
                {
                    ApiUrl = Configuration.GetValue<string>("OpenFGA:ApiUrl")!,
                };

                return new OpenFgaClient(clientConfiguration);
            });

            services.AddScoped<AclService>();

            ServiceProvider = services.BuildServiceProvider();
        }

        [TestInitialize]
        public virtual async Task TestInitializeAsync()
        {
            ServiceScope = ServiceProvider.CreateScope();

            await PreparePostgresAsync(default);
            await PrepareOpenFgaAsync(default);

            CurrentUser = await CreateCurrentUserAsync(email: "philipp@bytefish.de", roles: [Roles.Administrator, Roles.User]);
        }

        protected async Task<CurrentUser> CreateCurrentUserAsync(string email, string[] roles)
        {
            var claims = await ServiceProvider.GetRequiredService<UserService>().GetClaimsAsync(
                email: email,
                roles: roles,
                cancellationToken: default);

            var user = await ServiceProvider.GetRequiredService<UserService>().GetUserByEmailAsync(
                email: email,
                cancellationToken: default);

            return new CurrentUser
            {
                Principal = new ClaimsPrincipal(new ClaimsIdentity(claims)),
                User = user
            };
        }

        private async Task PreparePostgresAsync(CancellationToken cancellationToken)
        {
            await GetRequiredService<ApplicationDbContext>().Database
                .ExecuteSqlRawAsync("call gitclub.cleanup_tests()", cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task PrepareOpenFgaAsync(CancellationToken cancellationToken)
        {
            // This is the OpenFGA Store under Test:
            var sourceStoreId = Configuration.GetValue<string>("OpenFGA:StoreId")!;

            // We always want to run Integration Tests on the latest Authorization Model:
            var sourceAuthorizationModel = await this
                .GetLatestAuthorizationModelByStoreIdAsync(sourceStoreId, cancellationToken)
                .ConfigureAwait(false);

            // Then create the Store to run the Tests with:
            var targetStoreResponse = await GetRequiredService<OpenFgaClient>()
                .CreateStore(new ClientCreateStoreRequest { Name = "Test" })
                .ConfigureAwait(false);

            var targetStoreId = targetStoreResponse.Id;

            var targetAuthorizationModel = await GetRequiredService<OpenFgaClient>().WriteAuthorizationModel(
                body: new ClientWriteAuthorizationModelRequest
                {
                    TypeDefinitions = sourceAuthorizationModel.TypeDefinitions,
                    SchemaVersion = sourceAuthorizationModel.SchemaVersion,
                    AdditionalProperties = sourceAuthorizationModel.AdditionalProperties,
                    Conditions = sourceAuthorizationModel.Conditions,
                },
                options: new ClientWriteOptions
                {
                    StoreId = targetStoreId
                },
                cancellationToken: cancellationToken);

            // This is a bit ugly, but it makes things easier ...
            GetRequiredService<OpenFgaClient>().StoreId = targetStoreId;
            GetRequiredService<OpenFgaClient>().AuthorizationModelId = targetAuthorizationModel.AuthorizationModelId;
        }

        private async Task<AuthorizationModel> GetLatestAuthorizationModelByStoreIdAsync(string storeId, CancellationToken cancellationToken)
        {
            // Create a new Store:
            var client = GetRequiredService<OpenFgaClient>();

            // Apparently the first Authorization Model is also the latest one:
            var authorizationModelsResponse = await client
                .ReadAuthorizationModels(new ClientReadAuthorizationModelsOptions { StoreId = storeId }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var latestAuthorizationModel = authorizationModelsResponse.AuthorizationModels.FirstOrDefault();

            if (latestAuthorizationModel == null)
            {
                throw new Exception($"No AuthorizationModel found for StoreId {storeId}");
            }

            return latestAuthorizationModel;
        }

        [TestCleanup]
        public async Task TestCleanupAsync()
        {
            // Let's hope we always get the same old OpenFgaClient from the current scope:
            await GetRequiredService<OpenFgaClient>()
                .DeleteStore()
                .ConfigureAwait(false);

            // Dispose the Scope and all Scoped Services associated with the test:
            ServiceScope.Dispose();
        }

        /// <summary>
        /// Resolves a Service from the current Scope.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        protected TService GetRequiredService<TService>()
            where TService : notnull
        {
            return ServiceScope.ServiceProvider.GetRequiredService<TService>();
        }

        /// <summary>
        /// Gets or sets the Current User.
        /// </summary>
        protected CurrentUser CurrentUser { get; set; } = null!;
    }
}