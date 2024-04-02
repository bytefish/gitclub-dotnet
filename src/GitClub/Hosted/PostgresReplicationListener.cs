// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Postgres.Wal;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using System.Text.Json;

namespace GitClub.Hosted
{
    /// <summary>
    /// This Listener waits for Data Change Events sent by Logical Replication.
    /// </summary>
    public class PostgresReplicationListener : BackgroundService
    {
        private readonly ILogger<PostgresReplicationListener> _logger;

        private readonly PostgresReplicationService _replicationService;

        public PostgresReplicationListener(ILogger<PostgresReplicationListener> logger, PostgresReplicationService replicationService)
        {
            _logger = logger;
            _replicationService = replicationService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.TraceMethodEntry();
            
            var jsonSerializerOptions = GetSerializerOptions();

            await foreach(var transaction in _replicationService.StartReplicationAsync(stoppingToken))
            {
                // Process the Received Transaction
                _logger.LogInformation("Received Transaction: {Transaction}", JsonSerializer.Serialize(transaction, jsonSerializerOptions));
            }
        }

        private JsonSerializerOptions GetSerializerOptions()
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            return options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }
    }
}