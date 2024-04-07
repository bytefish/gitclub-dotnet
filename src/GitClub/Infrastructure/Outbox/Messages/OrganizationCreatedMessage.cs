// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    public class OrganizationCreatedMessage
    {
        /// <summary>
        /// Gets or sets the Organization ID.
        /// </summary>
        [JsonPropertyName("organizationId")]
        public required int OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the Base RepositoryRole for Users.
        /// </summary>
        [JsonPropertyName("baseRepositoryRole")]
        public required BaseRepositoryRoleEnum BaseRepositoryRole { get; set; }

        [JsonPropertyName("userOrganizationRoles")]
        public List<AddedUserToOrganizationMessage> UserOrganizationRoles { get; set; } = [];
    }
}
