// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using GitClub.Database;
using GitClub.Infrastructure;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Constants;
using GitClub.Services;
using GitClub.Tests.Infrastructure;
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
        /// Current GitClub Application.
        /// </summary>
        private GitClubApplication Application { get; set; } = null!;

        /// <summary>
        /// Configuration.
        /// </summary>
        private IConfiguration Configuration => Application.Services.GetRequiredService<IConfiguration>();

        /// <summary>
        /// Service Scope.
        /// </summary>
        protected IServiceProvider Services => Application.Services;

        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static async Task ClassInitializeAsync(TestContext context)
        {
            await StartDockerContainers(default);
        }

        private static async Task StartDockerContainers(CancellationToken cancellationToken)
        {
            var openfga_network = new NetworkBuilder()
                .WithName("openfga")
                .WithDriver(NetworkDriver.Bridge)
                .Build();

            var postgres = new ContainerBuilder()
                .WithName("postgres")
                .WithImage("postgres:16")
                .WithNetwork(openfga_network)
                .WithPortBinding(hostPort: 5432, containerPort: 5432)
                // Mount Postgres Configuration and SQL Scripts 
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/docker/postgres.conf"), "/usr/local/etc/postgres/postgres.conf")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/sql/openfga.sql"), "/docker-entrypoint-initdb.d/1-openfga.sql")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/sql/gitclub.sql"), "/docker-entrypoint-initdb.d/2-gitclub.sql")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/sql/gitclub-versioning.sql"), "/docker-entrypoint-initdb.d/3-gitclub-versioning.sql")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/sql/gitclub-notifications.sql"), "/docker-entrypoint-initdb.d/4-gitclub-notifications.sql")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/sql/gitclub-replication.sql"), "/docker-entrypoint-initdb.d/5-gitclub-replication.sql")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/sql/gitclub-tests.sql"), "/docker-entrypoint-initdb.d/6-gitclub-tests.sql")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/sql/gitclub-data.sql"), "/docker-entrypoint-initdb.d/7-gitclub-data.sql")
                // Set Username and Password
                .WithEnvironment(new Dictionary<string, string>
                {
                    {"POSTGRES_USER", "postgres" },
                    {"POSTGRES_PASSWORD", "password" },
                })
                // Start Postgres with the given postgres.conf.
                .WithCommand([
                    "postgres",
                    "-c",
                    "config_file=/usr/local/etc/postgres/postgres.conf"
                ])
                // Wait until the Port is exposed.
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilPortIsAvailable(5432))
                .Build();

            var openfga_migrate = new ContainerBuilder()
                .WithName("openfga-migration")
                .WithImage("openfga/openfga:latest")
                .DependsOn(postgres)
                .WithNetwork(openfga_network)
                .WithEnvironment(new Dictionary<string, string>
                {
                    {"OPENFGA_DATASTORE_ENGINE", "postgres" },
                    {"OPENFGA_DATASTORE_URI", "postgres://postgres:password@postgres:5432/postgres?sslmode=disable&search_path=openfga" }
                })
                .WithCommand("migrate")
                .Build();

            var openfga_server = new ContainerBuilder()
                .WithName("openfga-server")
                .WithImage("openfga/openfga:latest")
                .DependsOn(openfga_migrate)
                .WithNetwork(openfga_network)
                .WithCommand("run")
                .WithPortBinding(hostPort: 8080, containerPort: 8080)
                .WithPortBinding(hostPort: 8081, containerPort: 8081)
                .WithPortBinding(hostPort: 3000, containerPort: 3000)
                .WithEnvironment(new Dictionary<string, string>
                {
                    {"OPENFGA_DATASTORE_ENGINE", "postgres" },
                    {"OPENFGA_DATASTORE_URI", "postgres://postgres:password@postgres:5432/postgres?sslmode=disable&search_path=openfga" }
                })
                .Build();

            var openfga_model = new ContainerBuilder()
                .WithName("openfga-model")
                .WithImage("openfga/cli:latest")
                .DependsOn(openfga_server)
                .WithNetwork(openfga_network)
                .WithEnvironment(new Dictionary<string, string>
                {
                    {"FGA_STORE_ID", "01HP82R96XEJX1Q9YWA9XRQ4PM" }
                })
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/fga/gitclub.fga.yaml"), "/gitclub.fga.yaml")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/fga/gitclub-model.fga"), "/gitclub-model.fga")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/fga/gitclub-tuples.yaml"), "/gitclub-tuples.yaml")
                .WithCommand([
                    "store",
                    "import",
                    "--api-url", "http://openfga-server:8080",
                    "--file", "/gitclub.fga.yaml",
                    "--store-id", "01HP82R96XEJX1Q9YWA9XRQ4PM"
                ])   
                .Build();

            await postgres.StartAsync()
                .ConfigureAwait(false);

            await openfga_migrate.StartAsync()
                .ConfigureAwait(false);

            await openfga_server.StartAsync()
                .ConfigureAwait(false);

            await openfga_model.StartAsync()
                .ConfigureAwait(false);
        }

        [TestInitialize]
        public virtual async Task TestInitializeAsync()
        {
            Application = new GitClubApplication();

            await PreparePostgresAsync(default);
            await PrepareOpenFgaAsync(default);

            CurrentUser = await CreateCurrentUserAsync(email: "philipp@bytefish.de", roles: [Roles.Administrator, Roles.User]);
        }

        protected async Task<CurrentUser> CreateCurrentUserAsync(string email, string[] roles)
        {
            var claims = await Application.Services.GetRequiredService<UserService>().GetClaimsAsync(
                email: email,
                roles: roles,
                cancellationToken: default);

            var user = await Application.Services.GetRequiredService<UserService>().GetUserByEmailAsync(
                email: email,
                cancellationToken: default);

            return new CurrentUser
            {
                Principal = new ClaimsPrincipal(new ClaimsIdentity(claims)),
                User = user
            };
        }

        protected async Task ProcessAllOutboxEventsAsync()
        {
            var outboxEventProcessor = GetRequiredService<OutboxEventProcessor>();

            await outboxEventProcessor
                .ProcessAllOutboxEvents(default)
                .ConfigureAwait(false);
        }

        private async Task PreparePostgresAsync(CancellationToken cancellationToken)
        {
            var dbContextFactory = GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

            using var applicationDbContext = await dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            await applicationDbContext.Database
                .ExecuteSqlRawAsync("call gitclub.cleanup_tests()", cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task PrepareOpenFgaAsync(CancellationToken cancellationToken)
        {
            // This is the OpenFGA Store under Test:
            //01HP82R96XEJX1Q9YWA9XRQ4PM
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
            await Application.DisposeAsync();
        }

        /// <summary>
        /// Resolves a Service from the current Scope.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        protected TService GetRequiredService<TService>()
            where TService : notnull
        {
            return Services.GetRequiredService<TService>();
        }

        /// <summary>
        /// Gets or sets the Current User.
        /// </summary>
        protected CurrentUser CurrentUser { get; set; } = null!;
    }
}