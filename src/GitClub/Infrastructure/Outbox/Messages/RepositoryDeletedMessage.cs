// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Messages;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    public class RepositoryDeletedMessage
    {
        /// <summary>
        /// Gets or sets the Repository ID.
        /// </summary>
        [JsonPropertyName("repositoryId")]
        public required int RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the Repository Roles.
        /// </summary>
        [JsonPropertyName("userRepositoryRoles")]
        public List<RemovedUserFromRepositoryMessage> UserRepositoryRoles { get; set; } = [];

        /// <summary>
        /// Gets or sets the Repository Roles.
        /// </summary>
        [JsonPropertyName("teamRepositoryRoles")]
        public List<RemovedTeamFromRepositoryMessage> TeamRepositoryRoles { get; set; } = [];
    }
}
