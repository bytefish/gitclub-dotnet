// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    public class RepositoryUpdatedMessage
    {
        /// <summary>
        /// Gets or sets the Repository ID.
        /// </summary>
        [JsonPropertyName("repositoryId")]
        public required int RepositoryId { get; set; }
    }
}
