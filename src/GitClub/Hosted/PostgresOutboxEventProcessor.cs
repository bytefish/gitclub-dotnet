// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Outbox.Consumer;
using GitClub.Infrastructure.Outbox.Stream;
using GitClub.Infrastructure.Postgres.Wal;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using System.Text.Json;

namespace GitClub.Hosted
{
    public class PostgresOutboxEventProcessorOptions
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
    /// Processes Outbox Events.
    /// </summary>
    public class PostgresOutboxEventProcessor : BackgroundService
    {
        private readonly ILogger<PostgresOutboxEventProcessor> _logger;

        private readonly PostgresOutboxEventProcessorOptions _options;
        private readonly OutboxEventConsumer _outboxEventConsumer;

        public PostgresOutboxEventProcessor(ILogger<PostgresOutboxEventProcessor> logger, IOptions<PostgresOutboxEventProcessorOptions> options, OutboxEventConsumer outboxEventConsumer)
        {
            _logger = logger;
            _options = options.Value;
            _outboxEventConsumer = outboxEventConsumer;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // We could also inject the Stream, but I think it's better to do it 
            // this way, in case we have multiple consumers. I also played with 
            // putting it in a static method... feels wrong.
            var outboxEventStreamOptions = new PostgresOutboxEventStreamOptions
            {
                ConnectionString = _options.ConnectionString,
                PublicationName = _options.PublicationName,
                ReplicationSlotName = _options.ReplicationSlotName,
                OutboxEventSchemaName = _options.OutboxEventSchemaName,
                OutboxEventTableName = _options.OutboxEventTableName
            };

            var outboxEventStream = new PostgresOutboxEventStream(_logger, Options.Create(outboxEventStreamOptions));

            // Listen to the Outbox Event Stream.
            await foreach(var outboxEvent in outboxEventStream.StartOutboxEventStream(cancellationToken))
            {
                _logger.LogInformation("Received Transaction: {Transaction}", JsonSerializer.Serialize(transaction, jsonSerializerOptions));

                await _outboxEventConsumer
                    .HandleOutboxEventAsync(outboxEvent, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private JsonSerializerOptions GetJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            return options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }
    }
}