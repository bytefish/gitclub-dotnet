// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Outbox.Messages
{
    public class OrganizationUpdatedMessage
    {
        /// <summary>
        /// Gets or sets the Organization ID.
        /// </summary>
        [JsonPropertyName("organizationId")]
        public required int OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the Previous Base Repository Role, before Update.
        /// </summary>
        [JsonPropertyName("oldBaseRepositoryRole")]
        public required BaseRepositoryRoleEnum OldBaseRepositoryRole { get; set; }

        /// <summary>
        /// Gets or sets the new Base Repository Role, after Update.
        /// </summary>
        [JsonPropertyName("newBaseRepositoryRole")]
        public required BaseRepositoryRoleEnum NewBaseRepositoryRole { get; set; }

    }
}
