// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Infrastructure.Postgres
{
    /// <summary>
    /// Logs all Notifications received from a Postgres Channel.
    /// </summary>
    public class LoggingPostgresNotificationHandler : IPostgresNotificationHandler
    {
        private readonly ILogger<LoggingPostgresNotificationHandler> _logger;

        public LoggingPostgresNotificationHandler(ILogger<LoggingPostgresNotificationHandler> logger)
        {
            _logger = logger;
        }

        public ValueTask HandleNotificationAsync(PostgresNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogDebug("PostgresNotification (PID = {PID}, Channel = {Channel}, Payload = {Payload}",
                notification.PID, notification.Channel, notification.Payload);

            return ValueTask.CompletedTask;
        }
    }
}
