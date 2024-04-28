// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace GitClub.Tests
{
    public class DockerContainers
    {
        public static INetwork? OpenFgaNetwork;

        public static IContainer? PostgresContainer;

        public static IContainer? OpenFgaMigrateContainer;

        public static IContainer? OpenFgaServerContainer;

        public static IContainer? OpenFgaModelContainer;

        public static async Task StartAllContainersAsync()
        {
            OpenFgaNetwork = new NetworkBuilder()
                .WithName("openfga")
                .WithDriver(NetworkDriver.Bridge)
                .Build();

            PostgresContainer = new ContainerBuilder()
                .WithName("postgres")
                .WithImage("postgres:16")
                .WithNetwork(OpenFgaNetwork)
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

            OpenFgaMigrateContainer = new ContainerBuilder()
                .WithName("openfga-migration")
                .WithImage("openfga/openfga:latest")
                .DependsOn(PostgresContainer)
                .WithNetwork(OpenFgaNetwork)
                .WithEnvironment(new Dictionary<string, string>
                {
                    {"OPENFGA_DATASTORE_ENGINE", "postgres" },
                    {"OPENFGA_DATASTORE_URI", "postgres://postgres:password@postgres:5432/postgres?sslmode=disable&search_path=openfga" }
                })
                .WithCommand("migrate")
                .Build();

            OpenFgaServerContainer = new ContainerBuilder()
                .WithName("openfga-server")
                .WithImage("openfga/openfga:latest")
                .DependsOn(OpenFgaMigrateContainer)
                .WithNetwork(OpenFgaNetwork)
                .WithCommand("run")
                .WithPortBinding(hostPort: 8080, containerPort: 8080)
                .WithPortBinding(hostPort: 8081, containerPort: 8081)
                .WithPortBinding(hostPort: 3000, containerPort: 3000)
                .WithEnvironment(new Dictionary<string, string>
                {
                    {"OPENFGA_DATASTORE_ENGINE", "postgres" },
                    {"OPENFGA_DATASTORE_URI", "postgres://postgres:password@postgres:5432/postgres?sslmode=disable&search_path=openfga" }
                })
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilMessageIsLogged("HTTP server listening on '0.0.0.0:8080'.."))
                .Build();

            OpenFgaModelContainer = new ContainerBuilder()
                .WithName("openfga-model")
                .WithImage("openfga/cli:latest")
                .DependsOn(OpenFgaServerContainer)
                .WithNetwork(OpenFgaNetwork)
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

            await PostgresContainer.StartAsync();
            await OpenFgaMigrateContainer.StartAsync();
            await OpenFgaServerContainer.StartAsync();
            await OpenFgaModelContainer.StartAsync();
        }

        public static async Task StopAllContainersAsync()
        {
            if (PostgresContainer != null)
            {
                await PostgresContainer.StopAsync();
            }

            if (OpenFgaMigrateContainer != null)
            {
                await OpenFgaMigrateContainer.StopAsync();
            }

            if (OpenFgaServerContainer != null)
            {
                await OpenFgaServerContainer.StopAsync();
            }

            if (OpenFgaModelContainer != null)
            {
                await OpenFgaModelContainer.StopAsync();
            }
        }
    }
}
