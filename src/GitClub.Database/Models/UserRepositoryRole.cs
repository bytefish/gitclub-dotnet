// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Database.Models
{
    public class UserRepositoryRole : Entity
    {
        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the RepositoryId.
        /// </summary>
        public int RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public required RepositoryRoleEnum Role { get; set; }
    }
}
