// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using GitClub.Database;

namespace GitClub.Tests
{
    /// <summary>
    /// Will be used by all integration tests, that need an <see cref="ApplicationDbContext"/>. The Scope is "Per Test", 
    /// so you get a "fresh" set of services registered as "Scoped" for each and every new test. It's questionable, if 
    /// this holds for every test.
    /// </summary>
    [TestClass]
    public class TestContainersTest
    {
        [TestInitialize]
        public async Task TestInitializeAsync()
        {
            var network = new NetworkBuilder()
                .WithName("openfga")
                .Build();

            var postgres = new ContainerBuilder()
                .WithName("postgres")
                .WithImage("postgres:16")
                .WithNetwork(network)
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
                .WithNetwork(network)
                .WithEnvironment(new Dictionary<string, string>
                {
                    {"OPENFGA_DATASTORE_ENGINE", "postgres" },
                    {"OPENFGA_DATASTORE_URI", "postgres://postgres:password@postgres:5432/postgres?sslmode=disable&search_path=openfga" }
                })
                .Build();

            var openfga_server = new ContainerBuilder()
                .WithName("openfga-server")
                .WithImage("openfga/openfga:latest")
                .DependsOn(openfga_migrate)
                .WithNetwork(network)
                .WithCommand("run")
                .WithPortBinding(hostPort: 8080, containerPort: 8080)
                .WithPortBinding(hostPort: 8081, containerPort: 8081)
                .WithPortBinding(hostPort: 3000, containerPort: 3000)
                .WithEnvironment(new Dictionary<string, string>
                {
                    {"OPENFGA_DATASTORE_ENGINE", "postgres" },
                    {"OPENFGA_DATASTORE_URI", "postgres://postgres:password@postgres:5432/postgres?sslmode=disable&search_path=openfga" }
                })
                // Wait until the Port is exposed.
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilPortIsAvailable(8080))
                .Build();

            var openfga_model = new ContainerBuilder()
                .WithName("openfga-model")
                .WithImage("openfga/cli:latest")
                .DependsOn(openfga_server)
                .WithNetwork(network)
                .WithEnvironment(new Dictionary<string, string>
                {
                    {"FGA_STORE_ID", "01HP82R96XEJX1Q9YWA9XRQ4PM" }
                })
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/fga/gitclub.fga.yaml"), "/gitclub.fga.yaml")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/fga/gitclub-model.fga"), "/gitclub-model.fga")
                .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "Resources/fga/gitclub-tuples.yaml"), "/gitclub-tuples.yaml")
                .WithCommand("store import --api-url http://openfga:8080 --file /gitclub.fga.yaml --store-id ${FGA_STORE_ID}")
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

        [TestMethod]
        public void TestWaitForContainers()
        {
            // ...
        }
    }
}