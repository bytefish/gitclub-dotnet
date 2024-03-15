// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Database.Models
{
    public enum OrganizationRoleEnum
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Member.
        /// </summary>
        Member = 1,

        /// <summary>
        /// Billing Manager.
        /// </summary>
        BillingManager = 2,
    
        /// <summary>
        /// Owner.
        /// </summary>
        Owner = 3,
    }
}
