// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Messages
{
    /// <summary>
    /// A User has been added to an Organization in a given role.
    /// </summary>
    public class AddedUserToRepositoryMessage
    {
        /// <summary>
        /// Gets or sets the User ID.
        /// </summary>
        [JsonPropertyName("userId")]
        public required int UserId { get; set; }

        /// <summary>
        /// Gets or sets the Organization ID.
        /// </summary>
        [JsonPropertyName("repositoryId")]
        public required int RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the Role of the user.
        /// </summary>
        [JsonPropertyName("role")]
        public required RepositoryRoleEnum Role { get; set; }
    }
}