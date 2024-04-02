// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput;
using Npgsql.Replication.PgOutput.Messages;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace GitClub.Hosted
{
    /// <summary>
    /// A Transaction sent by Postgres with all related DataChange Events.
    /// </summary>
    public record ReplicationTransaction
    {
        public List<DataChangeEvent> ReplicationDataEvents { get; } = [];
    }

    /// <summary>
    /// Postgres send a so called Relation Message during the Logical Replication.
    /// </summary>
    public record Relation
    {
        /// <summary>
        /// Gets or sets the Id of the Relation.
        /// </summary>
        public required uint RelationId { get; set; }

        /// <summary>
        /// Gets or sets the Namespace.
        /// </summary>
        public required string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the Name of the Relation.
        /// </summary>
        public required string RelationName { get; set; }

        /// <summary>
        /// Gets or sets the Server Clock time.
        /// </summary>
        public required DateTime ServerClock { get; set; }

        /// <summary>
        /// Gets or sets the Column Names.
        /// </summary>
        public required string[] ColumnNames { get; set; }
    }

    /// <summary>
    /// Emitted, when a change to the Postgres tables occurs.
    /// </summary>
    public record DataChangeEvent
    {
        /// <summary>
        /// Gets or sets the Relation.
        /// </summary>
        public required Relation Relation { get; set; }
    }

    /// <summary>
    /// An insert event includes the new values.
    /// </summary>
    public record InsertDataChangeEvent : DataChangeEvent
    {
        /// <summary>
        /// Gets or sets the new column values.
        /// </summary>
        public required IDictionary<string, object?> NewValues { get; set; }
    }

    /// <summary>
    /// A default update event only contains the new values.
    /// </summary>
    public record DefaultUpdateDataChangeEvent : DataChangeEvent
    {
        public required IDictionary<string, object?> NewValues { get; set; }
    }

    /// <summary>
    /// A full update event contains the old and the new values.
    /// </summary>
    public record FullUpdateDataChangeEvent : DataChangeEvent
    {
        /// <summary>
        /// Gets or sets the new column values.
        /// </summary>
        public required IDictionary<string, object?> NewValues { get; set; }

        /// <summary>
        /// Gets or sets the old column values.
        /// </summary>
        public required IDictionary<string, object?> OldValues { get; set; }
    }

    /// <summary>
    /// A key delete event contains only the keys, that have been deleted.
    /// </summary>
    public record KeyDeleteDataChangeEvent : DataChangeEvent
    {
        /// <summary>
        /// Gets or sets the keys having been deleted.
        /// </summary>
        public required IDictionary<string, object?> Keys { get; set; }
    }

    /// <summary>
    /// A delete event contains the old column values.
    /// </summary>
    public record DeleteDataChangeEvent : DataChangeEvent
    {
        /// <summary>
        /// Gets or sets the old column values.
        /// </summary>
        public required IDictionary<string, object?> OldValues { get; set; }
    }

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
    public class PostgresWalService
    {
        private readonly ILogger<PostgresWalService> _logger;

        private readonly PostgresWalServiceOptions _options;

        public PostgresWalService(ILogger<PostgresWalService> logger,  IOptions<PostgresWalServiceOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async IAsyncEnumerable<ReplicationTransaction> StartReplicationAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // Connection to subscribe to the logical replication slot. We are 
            // using NodaTime, but LogicalReplicationConnection has no TypeMappers, 
            // so we need to add them globally...
#pragma warning disable CS0618 // Type or member is obsolete
            NpgsqlConnection.GlobalTypeMapper.UseNodaTime();
#pragma warning restore CS0618 // Type or member is obsolete

            var replicationConnection = new LogicalReplicationConnection(_options.ConnectionString);

            // Open the Connection.
            await replicationConnection
                .Open(cancellationToken)
                .ConfigureAwait(false);

            // Reference to the Publication.
            var replicationPublication = new PgOutputReplicationOptions(_options.PublicationName, 1, binary: true);

            // Reference to the Replication Slot.
            var replicationSlot = new PgOutputReplicationSlot(_options.ReplicationSlotName);
            
            // Postgres expects us to cache all relations.
            var relations = new ConcurrentDictionary<uint, Relation>();

            ReplicationTransaction transaction = null!;

            await foreach (var message in replicationConnection
                .StartReplication(replicationSlot, replicationPublication, cancellationToken)
                .ConfigureAwait(false))
            {
                _logger.LogDebug("Received Postgres WAL Message (Type = {WalMessageType}, ServerClock = {WalServerClock}, WalStart = {WalStart}, WalEnd = {WalEnd})",
                    message.GetType().Name, message.ServerClock, message.WalStart, message.WalEnd);

                if (message is BeginMessage beginMessage) {
                    transaction = new ReplicationTransaction();
                    break;
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
                else if(message is DefaultUpdateMessage defaultUpdateMessage)
                {
                    var relation = relations[defaultUpdateMessage.Relation.RelationId];

                    transaction.ReplicationDataEvents.Add(new DefaultUpdateDataChangeEvent
                    {
                        Relation = relation,
                        NewValues = await ReadColumnValuesAsync(relation, defaultUpdateMessage.NewRow, cancellationToken).ConfigureAwait(false),
                    });
                }
                else if(message is FullUpdateMessage fullUpdateMessage)
                {
                    var relation = relations[fullUpdateMessage.Relation.RelationId];

                    transaction.ReplicationDataEvents.Add(new FullUpdateDataChangeEvent
                    {
                        Relation = relation,
                        NewValues = await ReadColumnValuesAsync(relation, fullUpdateMessage.NewRow, cancellationToken).ConfigureAwait(false),
                        OldValues = await ReadColumnValuesAsync(relation, fullUpdateMessage.OldRow, cancellationToken).ConfigureAwait(false)
                    });
                }
                else if(message is KeyDeleteMessage keyDeleteMessage)
                {
                    var relation = relations[keyDeleteMessage.Relation.RelationId];

                    transaction.ReplicationDataEvents.Add(new KeyDeleteDataChangeEvent
                    {
                        Relation = relation,
                        Keys = await ReadColumnValuesAsync(relation, keyDeleteMessage.Key, cancellationToken).ConfigureAwait(false)
                    });
                }
                else if(message is FullDeleteMessage fullDeleteMessage)
                {
                    var relation = relations[fullDeleteMessage.Relation.RelationId];
                    
                    transaction.ReplicationDataEvents.Add(new DeleteDataChangeEvent
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

            async ValueTask<IDictionary<string, object?>> ReadColumnValuesAsync(Relation relation, ReplicationTuple replicationTuple, CancellationToken cancellationToken)
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
}