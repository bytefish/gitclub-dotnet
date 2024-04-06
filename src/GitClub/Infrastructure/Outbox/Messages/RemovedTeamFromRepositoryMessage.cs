// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Messages
{
    /// <summary>
    /// A User has been added to a Team.
    /// </summary>
    public class RemovedTeamFromRepositoryMessage
    {
        /// <summary>
        /// Gets or sets the Team ID.
        /// </summary>
        [JsonPropertyName("teamId")]
        public required int TeamId { get; set; }

        /// <summary>
        /// Gets or sets the User ID.
        /// </summary>
        [JsonPropertyName("repositoryId")]
        public required int RepositoryId { get; set; }
    }
}