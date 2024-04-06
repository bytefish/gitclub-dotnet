// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Messages
{
    /// <summary>
    /// A User has been added to a Team.
    /// </summary>
    public class AddedUserToTeamMessage
    {
        /// <summary>
        /// Gets or sets the User ID.
        /// </summary>
        [JsonPropertyName("userId")]
        public required int UserId { get; set; }

        /// <summary>
        /// Gets or sets the Team ID.
        /// </summary>
        [JsonPropertyName("teamId")]
        public required int TeamId { get; set; }

        /// <summary>
        /// Gets or sets the Role of the User in a Team.
        /// </summary>
        [JsonPropertyName("role")]
        public required TeamRoleEnum Role { get; set; }
    }
}