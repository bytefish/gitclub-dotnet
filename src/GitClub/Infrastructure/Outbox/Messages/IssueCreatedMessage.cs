// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    /// <summary>
    /// An Issue has been created.
    /// </summary>
    public class IssueCreatedMessage
    {
        /// <summary>
        /// Gets or sets the Issue ID.
        /// </summary>
        [JsonPropertyName("issueId")]
        public required int IssueId { get; set; }
        
        /// <summary>
        /// Gets or sets the Repository ID.
        /// </summary>
        [JsonPropertyName("repositoryId")]
        public required int RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the initial User to Issue assignments.
        /// </summary>
        [JsonPropertyName("userIssueRoles")]
        public required List<AddedUserToIssueMessage> UserIssueRoles { get; set; }
    }
}
