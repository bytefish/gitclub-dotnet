﻿namespace GitClub.Infrastructure.Postgres
{
    /// <summary>
    /// Handles Notifications received from a Postgres Channel.
    /// </summary>
    public interface IPostgresNotificationHandler
    {
        /// <summary>
        /// Handles a Notification received from a Postgres Channel.
        /// </summary>
        /// <param name="notification">Notification received from Postgres</param>
        /// <param name="cancellationToken">CancellationToken to stop asynchronous processing</param>
        /// <returns>Awaitable ValueTask</returns>
        ValueTask HandleNotificationAsync(PostgresNotification notification, CancellationToken cancellationToken);
    }

}
