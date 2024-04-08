// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Postgres.Wal;
using GitClub.Infrastructure.Postgres.Wal.Models;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
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
                        var outboxEvent = await MapToOutboxEventAsync(insertDataChangeEvent.Relation, insertDataChangeEvent.NewValues, cancellationToken).ConfigureAwait(false);
                        
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

        /// <summary>
        /// Tries to map the <see cref="Relation"/> and <see cref="IDictionary{TKey, TValue}"/>, that has been 
        /// extracted from the Postgres Replication Stream, into an <see cref="OutboxEvent"/>. We don't want the 
        /// service to fail, if this conversion fails, so wrap it inside a try-catch.
        /// </summary>
        /// <param name="relation">Relation for the OutboxEvent</param>
        /// <param name="values">Column Values received by the Postgres ReplicationTuple</param>
        /// <param name="cancellationToken">Cancellation Token to stop asynchronous processing</param>
        /// <returns>An <see cref="OutboxEvent">, or <see cref="null"/> if the mapping failed</returns>
        private async ValueTask<OutboxEvent?> MapToOutboxEventAsync(Relation relation, IDictionary<string, object?> values, CancellationToken cancellationToken)
        {
            try
            {
                var result = await InternalMapToOutboxEventAsync(relation, values, cancellationToken).ConfigureAwait(false);

                return result;
            } 
            catch(Exception e)
            {
                _logger.LogError(e, "Failed to deserialize OutboxEvent due to an Exception");

                return null;
            }
        }

        /// <summary>
        /// Maps the <see cref="Relation"/> and <see cref="IDictionary{TKey, TValue}"/>, that has been extracted from 
        /// the Postgres Replication Stream, into an <see cref="OutboxEvent"/>. This throws if anything with the mapping 
        /// goes wrong.
        /// </summary>
        /// <param name="relation">Relation for the OutboxEvent</param>
        /// <param name="values">Column Values received by the Postgres ReplicationTuple</param>
        /// <param name="cancellationToken">Cancellation Token to stop asynchronous processing</param>
        /// <returns>An <see cref="OutboxEvent">, or <see cref="null"/> if the mapping failed</returns>
        private ValueTask<OutboxEvent?> InternalMapToOutboxEventAsync(Relation relation, IDictionary<string, object?> values, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var jsonDocument = GetAsJsonDocument(values, "payload");

            if(jsonDocument == null)
            {
                _logger.LogWarning("Failed to deserialize Event Payload as JsonDocument");

                return ValueTask.FromResult<OutboxEvent?>(null);
            }

            var outboxEvent = new OutboxEvent
            {
                Id = DictionaryUtils.GetRequiredValue<int>(values, "outbox_event_id"),
                EventSource = DictionaryUtils.GetRequiredValue<string>(values, "event_source"),
                EventType = DictionaryUtils.GetRequiredValue<string>(values, "event_type"),
                EventTime = DictionaryUtils.GetRequiredValue<Instant>(values, "event_time").ToDateTimeOffset(),
                Payload = jsonDocument,
                CorrelationId1 = DictionaryUtils.GetOptionalValue<string>(values, "correlation_id_1"),
                CorrelationId2 = DictionaryUtils.GetOptionalValue<string>(values, "correlation_id_2"),
                CorrelationId3 = DictionaryUtils.GetOptionalValue<string>(values, "correlation_id_3"),
                CorrelationId4 = DictionaryUtils.GetOptionalValue<string>(values, "correlation_id_4"),
                LastEditedBy = DictionaryUtils.GetRequiredValue<int>(values, "last_edited_by"),
                SysPeriod = DictionaryUtils.GetRequiredValue<Interval>(values, "sys_period")
            };

            return ValueTask.FromResult<OutboxEvent?>(outboxEvent);

            JsonDocument? GetAsJsonDocument(IDictionary<string, object?> values, string key)
            {
                var json = DictionaryUtils.GetRequiredValue<string>(values, key);

                return JsonSerializer.Deserialize<JsonDocument>(json, _jsonSerializerOptions);
            }
        }
    }
}