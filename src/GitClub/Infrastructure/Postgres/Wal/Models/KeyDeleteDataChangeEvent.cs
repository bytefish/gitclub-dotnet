// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Infrastructure.Postgres.Wal.Models
{
    /// <summary>
    /// A key delete event contains only the keys, that have been deleted.
    /// </summary>
    public record KeyDeleteDataChangeEvent : DataChangeEvent
    {
        /// <summary>
        /// Gets or sets the keys having been deleted.
        /// </summary>
        public required Dictionary<string, object?> Keys { get; set; }
    }
}