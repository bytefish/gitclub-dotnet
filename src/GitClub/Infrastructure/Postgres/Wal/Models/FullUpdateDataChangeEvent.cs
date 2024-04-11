// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Infrastructure.Postgres.Wal.Models
{
    /// <summary>
    /// A full update event contains the old and the new values.
    /// </summary>
    public record FullUpdateDataChangeEvent : DataChangeEvent
    {
        /// <summary>
        /// Gets or sets the new column values.
        /// </summary>
        public required Dictionary<string, object?> NewValues { get; set; }

        /// <summary>
        /// Gets or sets the old column values.
        /// </summary>
        public required Dictionary<string, object?> OldValues { get; set; }
    }
}