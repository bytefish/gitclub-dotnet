// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Infrastructure.Postgres.Wal.Models
{
    /// <summary>
    /// A delete event contains the old column values.
    /// </summary>
    public record FullDeleteDataChangeEvent : DataChangeEvent
    {
        /// <summary>
        /// Gets or sets the old column values.
        /// </summary>
        public required IDictionary<string, object?> OldValues { get; set; }
    }
}