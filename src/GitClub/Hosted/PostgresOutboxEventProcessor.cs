// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Outbox.Consumer;
using GitClub.Infrastructure.Outbox.Postgres;
using Microsoft.Extensions.Options;

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
            var outboxSubscriberOptions = new PostgresOutboxSubscriberOptions
            {
                ConnectionString = _options.ConnectionString,
                PublicationName = _options.PublicationName,
                ReplicationSlotName = _options.ReplicationSlotName,
                OutboxEventSchemaName = _options.OutboxEventSchemaName,
                OutboxEventTableName = _options.OutboxEventTableName
            };

            var outboxEventStream = new PostgresOutboxSubscriber(_logger, Options.Create(outboxSubscriberOptions));

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await foreach (var outboxEvent in outboxEventStream.StartOutboxEventStreamAsync(cancellationToken))
                    {
                        _logger.LogInformation("Processing OutboxEvent (Id = {OutboxEventId})", outboxEvent.Id);

                        try
                        {
                            await _outboxEventConsumer
                                .ConsumeOutboxEventAsync(outboxEvent, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Failed to handle the OutboxEvent due to an Exception (ID = {OutboxEventId})", outboxEvent.Id);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Logical Replication failed with an Error. Restarting the Stream.");

                    // Probably add some better Retry options ...
                    await Task
                        .Delay(200)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}