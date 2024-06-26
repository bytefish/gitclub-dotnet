﻿// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    /// <summary>
    /// A User has been removed from a Team.
    /// </summary>
    public class RemovedUserFromTeamMessage
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
        /// Gets or sets the Team ID.
        /// </summary>
        [JsonPropertyName("role")]
        public required TeamRoleEnum Role { get; set; }

    }
}
