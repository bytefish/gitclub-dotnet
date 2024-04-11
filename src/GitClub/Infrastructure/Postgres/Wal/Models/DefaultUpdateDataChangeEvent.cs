// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Infrastructure.Postgres.Wal.Models
{
    /// <summary>
    /// A default update event only contains the new values.
    /// </summary>
    public record DefaultUpdateDataChangeEvent : DataChangeEvent
    {
        public required Dictionary<string, object?> NewValues { get; set; }
    }
}