// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Infrastructure.Postgres.Wal.Models
{
    /// <summary>
    /// Postgres send a so called Relation Message during the Logical Replication.
    /// </summary>
    public record Relation
    {
        /// <summary>
        /// Gets or sets the Id of the Relation.
        /// </summary>
        public required uint RelationId { get; set; }

        /// <summary>
        /// Gets or sets the Namespace.
        /// </summary>
        public required string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the Name of the Relation.
        /// </summary>
        public required string RelationName { get; set; }

        /// <summary>
        /// Gets or sets the Server Clock time.
        /// </summary>
        public required DateTime ServerClock { get; set; }

        /// <summary>
        /// Gets or sets the Column Names.
        /// </summary>
        public required string[] ColumnNames { get; set; }
    }
}