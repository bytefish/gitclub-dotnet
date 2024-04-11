// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Postgres.Wal;
using GitClub.Infrastructure.Postgres.Wal.Models;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Npgsql.Replication.PgOutput.Messages;
using Npgsql.Replication.PgOutput;
using System.Diagnostics.CodeAnalysis;
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
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public PostgresOutboxEventStream(ILogger logger, IOptions<PostgresOutboxEventStreamOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _jsonSerializerOptions = new JsonSerializerOptions()
                .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
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

                    // This is not an insert ...
                    if(dataChangeEvent is not InsertDataChangeEvent)
                    {
                        _logger.LogDebug($"Expected a \"{typeof(DataChangeEvent).Name}\", but was \"{dataChangeEvent.GetType().Name}\"");

                        continue;
                    }

                    // This is the correct message
                    if (dataChangeEvent is InsertDataChangeEvent insertDataChangeEvent)
                    {
                        var outboxEvent = await ConvertToOutboxEventAsync(insertDataChangeEvent.NewValues, cancellationToken).ConfigureAwait(false);
                        
                        // If we failed to map the OutboxEvent something exceptional happened, and the Logs 
                        // need to be inspected. There is no chance to recover from this and it needs to be 
                        // analyzed by someone.
                        //
                        // Log a warning, so alarms go off in your monitoring.
                        if(outboxEvent == null)
                        {
                            var insertDataChangeEventAsJson = JsonSerializer.Serialize(insertDataChangeEvent, _jsonSerializerOptions);

                            _logger.LogWarning("Received an OutboxEvent, but failed to correctly map it. Event was {SerializedOutboxEvent}", insertDataChangeEventAsJson);

                            continue;
                        }
                        
                        yield return outboxEvent;
                    }
                }
            }
        }

        ValueTask<OutboxEvent> ConvertToOutboxEventAsync(Dictionary<string, object?> values, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var payload = GetRequiredValue<string>(values, "payload");

            var outboxEvent = new OutboxEvent
            {
                Id = GetRequiredValue<int>(values, "outbox_event_id"),
                CorrelationId1 = GetOptionalValue<string>(values, "correlation_id_1"),
                CorrelationId2 = GetOptionalValue<string>(values, "correlation_id_2"),
                CorrelationId3 = GetOptionalValue<string>(values, "correlation_id_3"),
                CorrelationId4 = GetOptionalValue<string>(values, "correlation_id_4"),
                EventType = GetRequiredValue<string>(values, "event_type"),
                EventSource = GetRequiredValue<string>(values, "event_source"),
                EventTime = GetRequiredValue<DateTimeOffset>(values, "event_time"),
                Payload = JsonSerializer.Deserialize<JsonDocument>(payload)!,
                LastEditedBy = GetRequiredValue<int>(values, "last_edited_by")
            };

            return ValueTask.FromResult(outboxEvent);
        }

        T GetRequiredValue<T>(Dictionary<string, object?> values, string key)
        {
            if (!values.ContainsKey(key))
            {
                throw new InvalidOperationException($"Value is required for key '{key}'");
            }

            if (values[key] is not T t)
            {
                throw new InvalidOperationException($"Value is not Type '{typeof(T).Name}'");
            }

            return t;
        }

        T? GetOptionalValue<T>(Dictionary<string, object?> values, string key, T? defaultValue = default)
        {
            if (!values.ContainsKey(key))
            {
                return defaultValue;
            }

            if (values[key] is T t)
            {
                return t;
            }

            return defaultValue;
        }
    }
}