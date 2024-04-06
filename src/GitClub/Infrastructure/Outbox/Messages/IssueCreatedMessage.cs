// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Messages
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
        /// Gets or sets the ID of the User.
        /// </summary>
        [JsonPropertyName("createdBy")]
        public required int CreatedBy { get; set; }
    }
}
