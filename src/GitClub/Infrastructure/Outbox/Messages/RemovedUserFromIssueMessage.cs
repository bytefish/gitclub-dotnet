// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    public class RemovedUserFromIssueMessage
    {
        /// <summary>
        /// Gets or sets the User ID.
        /// </summary>
        [JsonPropertyName("userId")]
        public required int UserId { get; set; }

        /// <summary>
        /// Gets or sets the Organization ID.
        /// </summary>
        [JsonPropertyName("issueId")]
        public required int IssueId { get; set; }

        /// <summary>
        /// Gets or sets the Role.
        /// </summary>
        [JsonPropertyName("role")]
        public required IssueRoleEnum Role { get; set; }

        
    }
}
