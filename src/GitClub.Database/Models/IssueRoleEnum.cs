// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Database.Models
{
    public enum IssueRoleEnum
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Creator.
        /// </summary>
        Creator = 1,

        /// <summary>
        /// Assignee.
        /// </summary>
        Assignee = 2,

        /// <summary>
        /// Owner.
        /// </summary>
        Owner = 3,

        /// <summary>
        /// Reader.
        /// </summary>
        Reader = 4,
        
        /// <summary>
        /// Writer.
        /// </summary>
        Writer = 5,
    }
}
