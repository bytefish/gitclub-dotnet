// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    public class TeamDeletedMessage
    {
        /// <summary>
        /// Gets or sets the Team ID.
        /// </summary>
        [JsonPropertyName("teamId")]
        public required int TeamId { get; set; }

        /// <summary>
        /// Gets or sets the TeamRoles to delete.
        /// </summary>
        public List<RemovedUserFromTeamMessage> UserTeamRoles { get; set; } = [];
    }
}
