// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Postgres.Wal.Models
{
    /// <summary>
    /// Emitted, when a change to the Postgres tables occurs.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(InsertDataChangeEvent), typeDiscriminator: "insert")]
    [JsonDerivedType(typeof(DefaultUpdateDataChangeEvent), typeDiscriminator: "default_update")]
    [JsonDerivedType(typeof(FullUpdateDataChangeEvent), typeDiscriminator: "full_update")]
    [JsonDerivedType(typeof(KeyDeleteDataChangeEvent), typeDiscriminator: "key_delete")]
    [JsonDerivedType(typeof(FullDeleteDataChangeEvent), typeDiscriminator: "full_delete")]
    public abstract record DataChangeEvent
    {
        /// <summary>
        /// Gets or sets the Relation.
        /// </summary>
        public required Relation Relation { get; set; }
    }
}