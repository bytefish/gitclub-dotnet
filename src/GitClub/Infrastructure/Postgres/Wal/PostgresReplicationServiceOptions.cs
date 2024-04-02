// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Hosted;

namespace GitClub.Infrastructure.Postgres.Wal
{
    /// <summary>
    /// Options to configure the <see cref="PostgresNotificationService"/>.
    /// </summary>
    public class PostgresReplicationServiceOptions
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
}