// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    /// <summary>
    /// A User has been added to an Organization.
    /// </summary>
    public class AddedUserToIssueMessage
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
        /// Gets or sets the Role of the User.
        /// </summary>
        [JsonPropertyName("role")]
        public required IssueRoleEnum Role { get; set; }
    }
}