using GitClub.Database.Models;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Postgres.Wal;
using GitClub.Infrastructure.Postgres.Wal.Models;
using Microsoft.Extensions.Options;
using NodaTime;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GitClub.Infrastructure.Outbox.Stream
{
    public class PostgresOutboxEventStreamOptions
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

        /// <summary>
        /// Gets or sets the Table the Outbox Events are written to.
        /// </summary>
        public required string OutboxEventTableName { get; set; }

        /// <summary>
        /// Gets or sets the Schema the Outbox Events are written to.
        /// </summary>
        public required string OutboxEventSchemaName { get; set; }
    }

    /// <summary>
    /// Provides a Stream of <see cref="OutboxEvent"/>, that are published by a Postgres database.
    /// </summary>
    public class PostgresOutboxEventStream
    {
        private readonly ILogger _logger;
        private readonly PostgresOutboxEventStreamOptions _options;

        public PostgresOutboxEventStream(ILogger logger, IOptions<PostgresOutboxEventStreamOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async IAsyncEnumerable<OutboxEvent> StartOutboxEventStream([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var replicationClientOptions = new PostgresReplicationClientOptions
            {
                ConnectionString = _options.ConnectionString,
                PublicationName = _options.PublicationName,
                ReplicationSlotName = _options.ReplicationSlotName
            };

            var _replicationService = new PostgresReplicationClient(_logger, Options.Create(replicationClientOptions));

            // This loop will emit whenever new Transactions are available:
            await foreach (var transaction in _replicationService
                .StartReplicationAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                // We will now inspect the Data Change Events:
                foreach(var dataChangeEvent in transaction.DataChangeEvents)
                {
                    _logger.LogDebug($"Processing Data Change Event (Type = {dataChangeEvent.GetType().FullName}, Schema = {dataChangeEvent.Relation.Namespace}, Table = {dataChangeEvent.Relation.RelationName}");

                    // This is the wrong namespace ...
                    if(!string.Equals(dataChangeEvent.Relation.Namespace, _options.OutboxEventSchemaName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _logger.LogDebug($"Expected Namespace \"{_options.OutboxEventSchemaName}\", but was \"{dataChangeEvent.Relation.Namespace}\"");
                        
                        continue;
                    }

                    // This is the wrong Table ...
                    if (!string.Equals(dataChangeEvent.Relation.RelationName, _options.OutboxEventTableName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _logger.LogDebug($"Expected Namespace \"{_options.OutboxEventTableName}\", but was \"{dataChangeEvent.Relation.RelationName}\"");

                        continue;
                    }

                    if(dataChangeEvent is not InsertDataChangeEvent)
                    {
                        _logger.LogDebug($"Expected a \"{typeof(DataChangeEvent).Name}\", but was \"{dataChangeEvent.GetType().Name}\"");

                        continue;
                    }

                    // This is the correct message
                    if (dataChangeEvent is InsertDataChangeEvent insertDataChangeEvent)
                    {
                        var outboxEvent = await MapToOutboxEventAsync(insertDataChangeEvent.Relation, insertDataChangeEvent.NewValues, cancellationToken).ConfigureAwait(false);

                        yield return outboxEvent;
                    }
                }
            }
        }

        public ValueTask<OutboxEvent> MapToOutboxEventAsync(Relation relation, IDictionary<string, object?> values, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var outboxEvent = new OutboxEvent
            {
                Id = DictionaryUtils.GetRequiredValue<int>(values, "outbox_event_id"),
                EventSource = DictionaryUtils.GetRequiredValue<string>(values, "event_source"),
                EventType = DictionaryUtils.GetRequiredValue<string>(values, "event_type"),
                EventTime = DictionaryUtils.GetRequiredValue<DateTimeOffset>(values, "event_time"),
                Payload = DictionaryUtils.GetRequiredValue<JsonDocument>(values, "payload"),
                CorrelationId1 = DictionaryUtils.GetOptionalValue<string>(values, "correlation_id_1"),
                CorrelationId2 = DictionaryUtils.GetOptionalValue<string>(values, "correlation_id_2"),
                CorrelationId3 = DictionaryUtils.GetOptionalValue<string>(values, "correlation_id_3"),
                CorrelationId4 = DictionaryUtils.GetOptionalValue<string>(values, "correlation_id_4"),
                LastEditedBy = DictionaryUtils.GetRequiredValue<int>(values, "last_edited_by"),
                SysPeriod = DictionaryUtils.GetRequiredValue<Interval>(values, "sys_period")
            };

            return ValueTask.FromResult(outboxEvent);
        }
    }
}