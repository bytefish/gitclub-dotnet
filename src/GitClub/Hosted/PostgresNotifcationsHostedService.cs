// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Postgres;
using Npgsql;
using System.Text.Json;
using System.Threading.Channels;

namespace GitClub.Hosted
{
    public class PostgresNotifcationsHostedService : BackgroundService
    {
        private static readonly string ChannelName = "core_db_event";

        private readonly ILogger<PostgresNotifcationsHostedService> _logger;

        private readonly NpgsqlDataSource _npgsqlDataSource;

        public PostgresNotifcationsHostedService(ILogger<PostgresNotifcationsHostedService> logger, NpgsqlDataSource npgsqlDataSource)
        {
            _logger = logger;
            _npgsqlDataSource = npgsqlDataSource;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Uh, now the queue being unbounded is pretty dangerous right? 
            var channel = Channel.CreateUnbounded<PostgresNotification>();

            // We running until either of them is stopped or runs dry ...
            await Task.WhenAny(SetupPostgresAsync(stoppingToken), ProcessChannelAsync(stoppingToken));

            // Initializes the Postgres Listener by issueing a LISTEN Command.
            async Task SetupPostgresAsync(CancellationToken cancellationToken)
            {
                using var connection = await _npgsqlDataSource
                    .OpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false);

                // If we receive a message from Postgres, we convert the Event 
                // to a PostgresNotification and put it on the Channel.
                connection.Notification += (sender, x) =>
                {
                    var notification = new PostgresNotification
                    {
                        Channel = x.Channel,
                        PID = x.PID,
                        Payload = x.Payload,
                    };

                    channel.Writer.TryWrite(notification);
                };

                // We register to the Notifications on the Channel.
                using (var command = new NpgsqlCommand($"LISTEN {ChannelName}", connection))
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                // And now we are putting the Connection into the Wait State,
                // until the Cancellation is requested.
                while (!cancellationToken.IsCancellationRequested)
                {
                    await connection.WaitAsync(cancellationToken);
                }
            }

            // This Processes the Messages received by the Channel, so we can process 
            // the messages. What we are doing is to basically deserialize the Payload 
            // to a PostgresJsonNotification.
            async Task ProcessChannelAsync(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
                        {
                            var jsonNotification = JsonSerializer.Deserialize<PostgresJsonNotification>(message.Payload);

                            // TODO Handle Notifications ...                            
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "An Error Occured processing the Event");
                    }
                }
            }
        }
    }
}