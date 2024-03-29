// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Postgres;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput;
using Npgsql.Replication.PgOutput.Messages;
using System.Collections.Concurrent;

namespace GitClub.Hosted
{
    /// <summary>
    /// Options to configure the <see cref="PostgresNotificationService"/>.
    /// </summary>
    public class PostgresWalServiceOptions
    {
        /// <summary>
        /// Gets or sets the ConnectionString for the Replication Stream.
        /// </summary>
        public required string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the PublicationName the Service is listening to.
        /// </summary>
        public required string PublicationName { get; set; }

        /// <summary>
        /// Gets or sets the ReplicationSlot the Service is listening to.
        /// </summary>
        public required string ReplicationSlotName { get; set; }
    }

    /// <summary>
    /// This Service waits for Notifications received on a given Postgres Channel name.
    /// </summary>
    public class PostgresWalService : BackgroundService
    {
        private readonly ILogger<PostgresWalService> _logger;

        private readonly PostgresWalServiceOptions _options;

        public PostgresWalService(ILogger<PostgresWalService> logger, IOptions<PostgresWalServiceOptions> options, NpgsqlDataSource npgsqlDataSource, IPostgresNotificationHandler postgresNotificationHandler)
        {
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // Connection to subscribe to the logical replication slot
            var replicationConnection = new LogicalReplicationConnection(_options.ConnectionString);

            // Open the Connection.
            await replicationConnection
                .Open(cancellationToken)
                .ConfigureAwait(false);

            // Reference to the Publication.
            var replicationPublication = new PgOutputReplicationOptions(_options.PublicationName, 1);

            // Reference to the Replication Slot.
            var replicationSlot = new PgOutputReplicationSlot(_options.ReplicationSlotName);

            await foreach (var message in replicationConnection
                .StartReplication(replicationSlot, replicationPublication, cancellationToken)
                .ConfigureAwait(false))
            {
                _logger.LogDebug("Received Postgres WAL Message (Type = {WalMessageType}, ServerClock = {WalServerClock}, WalStart = {WalStart}, WalEnd = {WalEnd})",
                    message.GetType().Name, message.ServerClock, message.WalStart, message.WalEnd);

                switch(message)
                {
                    case BeginMessage beginMessage:
                        break;
                    case CommitMessage commitMessage:
                        break;
                    case RelationMessage relationMessage:
                        break;
                    case InsertMessage insertMessage:
                        break;
                    case DefaultUpdateMessage defaultUpdateMessage:
                        break;
                    case FullUpdateMessage fullUpdateMessage:
                        break;
                    case KeyDeleteMessage keyDeleteMessage:
                        break;
                    case FullDeleteMessage fullDeleteMessage:
                        break;
                    default:
                        throw new InvalidOperationException($"Could not handle Message Type {message.GetType().Name}");
                }

                replicationConnection.SetReplicationStatus(message.WalEnd);
            }
        }
    }
}