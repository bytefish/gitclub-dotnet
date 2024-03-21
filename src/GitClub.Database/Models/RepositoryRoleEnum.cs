// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Database.Models
{
    public enum RepositoryRoleEnum
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Reader.
        /// </summary>
        Reader = 1,

        /// <summary>
        /// Triager.
        /// </summary>
        Triager = 2,

        /// <summary>
        /// Writer.
        /// </summary>
        Writer = 3,

        /// <summary>
        /// Maintainer.
        /// </summary>
        Maintainer = 4,

        /// <summary>
        /// Administrator.
        /// </summary>
        Administrator = 5,

        /// <summary>
        /// Owner.
        /// </summary>
        Owner = 6,
    }
}
