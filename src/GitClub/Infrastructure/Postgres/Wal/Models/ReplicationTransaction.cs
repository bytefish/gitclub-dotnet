// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Infrastructure.Postgres.Wal.Models
{
    /// <summary>
    /// A Transaction sent by Postgres with all related DataChange Events.
    /// </summary>
    public record ReplicationTransaction
    {
        public List<DataChangeEvent> DataChangeEvents { get; } = [];
    }
}