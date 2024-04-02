// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Infrastructure.Postgres.Wal.Models
{
    /// <summary>
    /// An insert event includes the new values.
    /// </summary>
    public record InsertDataChangeEvent : DataChangeEvent
    {
        /// <summary>
        /// Gets or sets the new column values.
        /// </summary>
        public required IDictionary<string, object?> NewValues { get; set; }
    }
}