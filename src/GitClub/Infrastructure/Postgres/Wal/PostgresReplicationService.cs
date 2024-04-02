// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Postgres.Wal.Models;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput;
using Npgsql.Replication.PgOutput.Messages;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace GitClub.Infrastructure.Postgres.Wal
{
    /// <summary>
    /// This Service processes Replication Events published by a Postgres publication.
    /// </summary>
    public class PostgresReplicationService
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<PostgresReplicationService> _logger;

        /// <summary>
        /// Options to configure the Wal Receiver.
        /// </summary>
        private readonly PostgresReplicationServiceOptions _options;

        /// <summary>
        /// Creates a new <see cref="PostgresReplicationService" />.
        /// </summary>
        /// <param name="logger">Logger to log messages</param>
        /// <param name="options">Options to configure the service</param>
        public PostgresReplicationService(ILogger<PostgresReplicationService> logger, IOptions<PostgresReplicationServiceOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Instructs the server to start the Logical Streaming Replication Protocol (pgoutput logical decoding 
        /// plugin), starting at WAL location walLocation or at the slot's consistent point if walLocation isn't 
        /// specified. The server can reply with an error, for example if the requested section of the WAL has 
        /// already been recycled.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token to stop the Logical Replication</param>
        /// <returns>Replication Transactions</returns>
        /// <exception cref="InvalidOperationException">Thrown when a replication message can't be handled</exception>
        public async IAsyncEnumerable<ReplicationTransaction> StartReplicationAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // Connection to subscribe to the logical replication slot. We are 
            // using NodaTime, but LogicalReplicationConnection has no TypeMappers, 
            // so we need to add them globally...
#pragma warning disable CS0618 // Type or member is obsolete
            NpgsqlConnection.GlobalTypeMapper.UseNodaTime();
#pragma warning restore CS0618 // Type or member is obsolete

            // This is the only way to create the Replication Connection, I have found no 
            // way to utilize the NpgsqlDataSource for it. There might be a way though.
            var replicationConnection = new LogicalReplicationConnection(_options.ConnectionString);

            // Open the Connection.
            await replicationConnection
                .Open(cancellationToken)
                .ConfigureAwait(false);

            // Reference to the Publication.
            var replicationPublication = new PgOutputReplicationOptions(_options.PublicationName, protocolVersion: 1, binary: true);

            // Reference to the Replication Slot.
            var replicationSlot = new PgOutputReplicationSlot(_options.ReplicationSlotName);

            // Postgres expects us to cache all relations.
            var relations = new ConcurrentDictionary<uint, Relation>();

            // The current transaction, which will be set, when we receive the first commit message.
            ReplicationTransaction transaction = null!;

            await foreach (var message in replicationConnection
                .StartReplication(replicationSlot, replicationPublication, cancellationToken)
                .ConfigureAwait(false))
            {
                _logger.LogDebug("Received Postgres WAL Message (Type = {WalMessageType}, ServerClock = {WalServerClock}, WalStart = {WalStart}, WalEnd = {WalEnd})",
                    message.GetType().Name, message.ServerClock, message.WalStart, message.WalEnd);

                if (message is BeginMessage beginMessage)
                {
                    transaction = new ReplicationTransaction();
                }
                else if (message is CommitMessage commitMessage)
                {
                    yield return transaction;
                }
                else if (message is RelationMessage relationMessage)
                {
                    relations[relationMessage.RelationId] = new Relation
                    {
                        RelationId = relationMessage.RelationId,
                        Namespace = relationMessage.Namespace,
                        RelationName = relationMessage.RelationName,
                        ServerClock = relationMessage.ServerClock,
                        ColumnNames = relationMessage.Columns
                            .Select(x => x.ColumnName)
                            .ToArray()
                    };
                }
                else if (message is InsertMessage insertMessage)
                {
                    var relation = relations[insertMessage.Relation.RelationId];

                    transaction.ReplicationDataEvents.Add(new InsertDataChangeEvent
                    {
                        Relation = relation,
                        NewValues = await ReadColumnValuesAsync(relation, insertMessage.NewRow, cancellationToken).ConfigureAwait(false)
                    });
                }
                else if (message is DefaultUpdateMessage defaultUpdateMessage)
                {
                    var relation = relations[defaultUpdateMessage.Relation.RelationId];

                    transaction.ReplicationDataEvents.Add(new DefaultUpdateDataChangeEvent
                    {
                        Relation = relation,
                        NewValues = await ReadColumnValuesAsync(relation, defaultUpdateMessage.NewRow, cancellationToken).ConfigureAwait(false),
                    });
                }
                else if (message is FullUpdateMessage fullUpdateMessage)
                {
                    var relation = relations[fullUpdateMessage.Relation.RelationId];

                    transaction.ReplicationDataEvents.Add(new FullUpdateDataChangeEvent
                    {
                        Relation = relation,
                        NewValues = await ReadColumnValuesAsync(relation, fullUpdateMessage.NewRow, cancellationToken).ConfigureAwait(false),
                        OldValues = await ReadColumnValuesAsync(relation, fullUpdateMessage.OldRow, cancellationToken).ConfigureAwait(false)
                    });
                }
                else if (message is KeyDeleteMessage keyDeleteMessage)
                {
                    var relation = relations[keyDeleteMessage.Relation.RelationId];

                    transaction.ReplicationDataEvents.Add(new KeyDeleteDataChangeEvent
                    {
                        Relation = relation,
                        Keys = await ReadColumnValuesAsync(relation, keyDeleteMessage.Key, cancellationToken).ConfigureAwait(false)
                    });
                }
                else if (message is FullDeleteMessage fullDeleteMessage)
                {
                    var relation = relations[fullDeleteMessage.Relation.RelationId];

                    transaction.ReplicationDataEvents.Add(new FullDeleteDataChangeEvent
                    {
                        Relation = relation,
                        OldValues = await ReadColumnValuesAsync(relation, fullDeleteMessage.OldRow, cancellationToken).ConfigureAwait(false)
                    });
                }
                else
                {
                    // We don't know what to do here and everything we could do... feels wrong. Throw 
                    // up to the consumer and let them handle the problem.
                    throw new InvalidOperationException($"Could not handle Message Type {message.GetType().Name}");
                }

                // Acknowledge the message.
                replicationConnection.SetReplicationStatus(message.WalEnd);
            }
        }

        private async ValueTask<IDictionary<string, object?>> ReadColumnValuesAsync(Relation relation, ReplicationTuple replicationTuple, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var results = new ConcurrentDictionary<string, object?>();

            // We need to track the current Column:
            int columnIdx = 0;

            // Each "ReplicationTuple" consists of multiple "ReplicationValues", that we could iterate over.
            await foreach (var replicationValue in replicationTuple)
            {
                // These "ReplicationValues" do not carry the column name, so we resolve the column name
                // from the associated relation. This is going to throw, if we cannot find the column name, 
                // but it should throw... because it is exceptional.
                var column = relation.ColumnNames[columnIdx];

                // Get the column value and let Npgsql decide, how to map the value. You could register
                // type mappers for the LogicalReplicationConnection, so you can also automagically map 
                // unknown types.
                //
                // This is going to throw, if Npgsql fails to read the values.
                var value = await replicationValue.Get(cancellationToken).ConfigureAwait(false);

                // If we fail to add the value to the Results, there is not much we can do. Log it 
                // and go ahead.
                if (!results.TryAdd(column, value))
                {
                    _logger.LogInformation("Failed to map ReplicationValue for Column {ColumnName}", column);
                }

                // Process next column
                columnIdx++;
            }

            return results;
        }
    }
}