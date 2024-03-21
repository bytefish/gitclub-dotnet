// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Database.Models
{
    public enum BaseRepositoryRoleEnum
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Reader.
        /// </summary>
        RepositoryReader = 1,

        /// <summary>
        /// Writer.
        /// </summary>
        RepositoryWriter = 2,

        /// <summary>
        /// Administrator.
        /// </summary>
        RepositoryAdministrator = 3
    }
}
