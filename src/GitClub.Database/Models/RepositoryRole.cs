// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Database.Models
{
    public class RepositoryRole : Entity
    {
        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the Description.
        /// </summary>
        public required string Description { get; set; }
    }
}
