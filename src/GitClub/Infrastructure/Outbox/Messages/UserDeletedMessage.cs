// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Outbox.Messages;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    /// <summary>
    /// A User has been deleted and all assignments need to be terminated.
    /// </summary>
    public class UserDeletedMessage
    {
        /// <summary>
        /// Gets or sets the User ID.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the Organization Roles to delete.
        /// </summary>
        [JsonPropertyName("userIssueRoles")]
        public List<RemovedUserFromIssueMessage> UserIssueRoles { get; set; } = [];
        
        /// <summary>
        /// Gets or sets the Organization Roles to delete.
        /// </summary>
        [JsonPropertyName("userOrganizationRoles")]
        public List<RemovedUserFromOrganizationMessage> UserOrganizationRoles { get; set; } = [];

        /// <summary>
        /// Gets or sets the Organization Roles to delete.
        /// </summary>
        [JsonPropertyName("userTeamRoles")]
        public List<RemovedUserFromTeamMessage> UserTeamRoles { get; set; } = [];

        /// <summary>
        /// Gets or sets the Organization Roles to delete.
        /// </summary>
        [JsonPropertyName("userRepositoryRoles")]
        public List<RemovedUserFromRepositoryMessage> UserRepositoryRoles { get; set; } = [];

    }
}
