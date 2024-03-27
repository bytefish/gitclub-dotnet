// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Services;
using GitClub.Infrastructure;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Errors;

namespace GitClub.Tests.Services
{
    [TestClass]
    public class OrganizationServiceTests : IntegrationTestBase
    {
        protected UserService UserService => GetRequiredService<UserService>();

        protected AclService AclService => GetRequiredService<AclService>();

        protected OrganizationService OrganizationService => GetRequiredService<OrganizationService>();

        /// <summary>
        /// Creates a new <see cref="Organization"> and creates the OpenFGA Tuples.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task CreateOrganizationAsync_Success()
        {
            // Arrange
            var values = new Organization
            {
                BaseRepositoryRole = BaseRepositoryRoleEnum.RepositoryReader,
                Name = "Unit Test",
                BillingAddress = "Billing Address",
                LastEditedBy = Users.GhostUserId
            };

            // Act
            var organization = await OrganizationService.CreateOrganizationAsync(values, CurrentUser, default);

            // Assert
            var userOrganizationRoleTuples = await AclService.ReadTuplesAsync<Organization, User>(organization.Id, null, CurrentUser.UserId, null).ToListAsync();

            Assert.AreEqual(1, userOrganizationRoleTuples.Count);
            Assert.AreEqual(Relations.Owner, userOrganizationRoleTuples[0].Relation);

            var organizationMemberOrganizationTuples = await AclService
                .ReadTuplesAsync<Organization, Organization>(organization.Id, null, organization.Id, Relations.Member)
                .ToListAsync();

            Assert.AreEqual(1, userOrganizationRoleTuples.Count);
            Assert.AreEqual(Relations.RepoReader, organizationMemberOrganizationTuples[0].Relation);
        }

        /// <summary>
        /// Creates a new <see cref="Organization">, deletes it and checks if all tuples have been deleted.
        /// </summary>
        [TestMethod]
        public async Task CreateAndDeleteOrganizationAsync_Success()
        {
            // Arrange
            var values = new Organization
            {
                BaseRepositoryRole = BaseRepositoryRoleEnum.RepositoryReader,
                Name = "Unit Test",
                BillingAddress = "Billing Address",
                LastEditedBy = Users.GhostUserId
            };

            // Act
            var organization = await OrganizationService.CreateOrganizationAsync(values, CurrentUser, default);

            await OrganizationService.DeleteOrganizationAsync(organization.Id, CurrentUser, default);

            // Assert
            var userOrganizationRoleTuples = await AclService.ReadTuplesAsync<Organization, User>(organization.Id, null, CurrentUser.UserId, null).ToListAsync();
            var organizationOrganizationRoleTuples = await AclService.ReadTuplesAsync<Organization, Organization>(organization.Id, null, organization.Id, Relations.Member).ToListAsync();

            Assert.AreEqual(0, userOrganizationRoleTuples.Count);
            Assert.AreEqual(0, organizationOrganizationRoleTuples.Count);
        }


        /// <summary>
        /// Creates a new <see cref="Organization">, creates a new <see cref="User"/>, assigns the user 
        /// to the organization and checks the OpenFGA tuples.
        /// </summary>
        [TestMethod]
        public async Task AddMemberToOrganizationAsync_Success()
        {
            // Arrange
            var organization = new Organization
            {
                BaseRepositoryRole = BaseRepositoryRoleEnum.RepositoryReader,
                Name = "Unit Test",
                BillingAddress = "Billing Address",
                LastEditedBy = Users.GhostUserId
            };

            var user = new User
            {
                Email = "test@test.local",
                PreferredName = "Test User",
                LastEditedBy = CurrentUser.UserId
            };

            // Act
            organization = await OrganizationService.CreateOrganizationAsync(organization, CurrentUser, default);
            user = await UserService.CreateUserAsync(user, CurrentUser, default);
            
            await OrganizationService.AddUserOrganizationRoleAsync(organization.Id, user.Id, OrganizationRoleEnum.Member, CurrentUser, default);

            // Assert
            var userOrganizationRoleTuples = await AclService.ReadTuplesAsync<Organization, User>(organization.Id, null, user.Id, null).ToListAsync();

            Assert.AreEqual(1, userOrganizationRoleTuples.Count);
            Assert.AreEqual(Relations.Member, userOrganizationRoleTuples[0].Relation);
        }

        /// <summary>
        /// Creates a new <see cref="Organization">, tries to assign the user twice 
        /// to the same organization. Fails with an Exception.
        /// </summary>
        [TestMethod]
        public async Task AddMemberToOrganizationAsync_AssignMultipleTimesToSameOrganization_FailsWithException()
        {
            // Arrange
            
            var organization = await OrganizationService.CreateOrganizationAsync(new Organization
            {
                BaseRepositoryRole = BaseRepositoryRoleEnum.RepositoryReader,
                Name = "Unit Test",
                BillingAddress = "Billing Address",
                LastEditedBy = Users.GhostUserId
            }, CurrentUser, default);
            
            var user = await UserService.CreateUserAsync(new User
            {
                Email = "test@test.local",
                PreferredName = "Test User",
                LastEditedBy = CurrentUser.UserId
            }, CurrentUser, default);

            // Act
            ApplicationErrorException? caught = null;

            try
            {
                await OrganizationService.AddUserOrganizationRoleAsync(organization.Id, user.Id, OrganizationRoleEnum.Member, CurrentUser, default);
                await OrganizationService.AddUserOrganizationRoleAsync(organization.Id, user.Id, OrganizationRoleEnum.Administrator, CurrentUser, default);
            } 
            catch(ApplicationErrorException e)
            {
                caught = e;
            }

            // Assert
            Assert.IsNotNull(caught);
            Assert.AreEqual(ErrorCodes.UserAlreadyAssignedToOrganization, caught.ErrorCode);
        }


        /// <summary>
        /// Creates a new <see cref="Organization">, creates a new <see cref="User"/>, assigns the user to the organization, 
        /// removes the user from the organization and checks the OpenFGA tuples.
        /// </summary>
        [TestMethod]
        public async Task RemoveUserOrganizationRoleAsync_Success()
        {
            // Arrange
            var user = await UserService.CreateUserAsync(new User
            {
                Email = "test@test.local",
                PreferredName = "Test User",
                LastEditedBy = CurrentUser.UserId
            }, CurrentUser, default);
            
            var organization = await OrganizationService.CreateOrganizationAsync(new Organization
            {
                BaseRepositoryRole = BaseRepositoryRoleEnum.RepositoryReader,
                Name = "Unit Test",
                BillingAddress = "Billing Address",
                LastEditedBy = Users.GhostUserId
            }, CurrentUser, default); ;

            // Act
            await OrganizationService.AddUserOrganizationRoleAsync(organization.Id, user.Id, OrganizationRoleEnum.Member, CurrentUser, default);
            await OrganizationService.RemoveUserOrganizationRoleAsync(organization.Id, user.Id, OrganizationRoleEnum.Member, CurrentUser, default);

            // Assert
            var userOrganizationRoleTuples = await AclService.ReadTuplesAsync<Organization, User>(organization.Id, null, user.Id, null).ToListAsync();

            Assert.AreEqual(0, userOrganizationRoleTuples.Count);
        }
    }
}
