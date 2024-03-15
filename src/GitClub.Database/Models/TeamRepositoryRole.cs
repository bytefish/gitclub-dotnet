// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Database.Models
{
    public class TeamRepositoryRole : Entity
    {
        /// <summary>
        /// Gets or sets the TeamId.
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        /// Gets or sets the RepositoryId.
        /// </summary>
        public int RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the Role.
        /// </summary>
        public required RepositoryRoleEnum Role { get; set; }
    }
}
