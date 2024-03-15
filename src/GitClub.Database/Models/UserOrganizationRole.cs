// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Database.Models
{
    public class UserOrganizationRole : Entity
    {
        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationId.
        /// </summary>
        public int OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the Name. 
        /// </summary>
        public required OrganizationRoleEnum Role { get; set; }
    }
}
