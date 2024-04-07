// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Database.Models
{
    public class UserIssueRole : Entity
    {
        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the IssueId.
        /// </summary>
        public int IssueId { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public required IssueRoleEnum Role { get; set; }
    }
}