// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    public class TeamCreatedMessage
    {
        /// <summary>
        /// Gets or sets the Team ID.
        /// </summary>
        [JsonPropertyName("teamId")]
        public required int TeamId { get; set; }
        
        /// <summary>
        /// Gets or sets the Organization ID.
        /// </summary>
        [JsonPropertyName("organizationId")]
        public required int OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the Team Roles for Users, when creating the Team.
        /// </summary>
        [JsonPropertyName("teamRoles")]
        public required List<AddedUserToTeamMessage> TeamRoles { get; set; }

    }
}
