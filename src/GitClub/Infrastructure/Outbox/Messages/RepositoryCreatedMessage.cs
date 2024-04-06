// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    public class RepositoryCreatedMessage
    {
        /// <summary>
        /// Gets or sets the Repository ID.
        /// </summary>
        [JsonPropertyName("repositoryId")]
        public required int RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the Organization ID.
        /// </summary>
        [JsonPropertyName("organizationId")]
        public required int OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the Users added when creating the Repository.
        /// </summary>
        [JsonPropertyName("userRepositoryRoles")]
        public List<AddedUserToRepositoryMessage> UserRepositoryRoles { get; set; } = [];
    }
}
