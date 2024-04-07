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

            await ProcessAllOutboxEventsAsync();

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
            await ProcessAllOutboxEventsAsync();
            
            user = await UserService.CreateUserAsync(user, CurrentUser, default);
            await ProcessAllOutboxEventsAsync();

            await OrganizationService.AddUserToOrganizationAsync(user.Id, organization.Id, OrganizationRoleEnum.Member, CurrentUser, default);
            await ProcessAllOutboxEventsAsync();

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

            await ProcessAllOutboxEventsAsync();

            // Act
            ApplicationErrorException? caught = null;

            try
            {
                await OrganizationService.AddUserToOrganizationAsync(user.Id, organization.Id, OrganizationRoleEnum.Member, CurrentUser, default);
                await ProcessAllOutboxEventsAsync();
                await OrganizationService.AddUserToOrganizationAsync(user.Id, organization.Id, OrganizationRoleEnum.Administrator, CurrentUser, default);
                await ProcessAllOutboxEventsAsync();

            }
            catch (ApplicationErrorException e)
            {
                caught = e;
            }

            // Assert
            Assert.IsNotNull(caught);
            Assert.AreEqual(ErrorCodes.UserAlreadyAssignedToOrganization, caught.ErrorCode);
        }

        /// <summary>
        /// Creates a new <see cref="Organization">, and adds a <see cref="User"/> as the Organizations Administrator. 
        /// </summary>
        [TestMethod]
        public async Task UpdateOrganizationAsync_InRoleAdministrator_CanUpdateOrganization()
        {
            // Arrange
            var testUser = await UserService.CreateUserAsync(new User
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
            }, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            await OrganizationService.AddUserToOrganizationAsync(testUser.Id, organization.Id, OrganizationRoleEnum.Administrator, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            var currentTestUser = await CreateCurrentUserAsync(testUser.Email, [Roles.User]);

            // Act
            var updatedOrganization = await OrganizationService.UpdateOrganizationAsync(organization.Id, new Organization
            {
                BaseRepositoryRole = BaseRepositoryRoleEnum.RepositoryWriter,
                Name = "My New Name",
                RowVersion = organization.RowVersion,
                LastEditedBy = currentTestUser.UserId,
            }, currentTestUser, default);

            await ProcessAllOutboxEventsAsync();

            // Assert
            Assert.AreEqual("My New Name", updatedOrganization.Name);
        }

        /// <summary>
        /// Cannot update an <see cref="Organization">, if not in Administrator Role.
        /// </summary>
        [TestMethod]
        public async Task UpdateOrganizationAsync_NotInRoleAdministrator_ExceptionWithErrorCode()
        {
            // Arrange
            var testUser = await UserService.CreateUserAsync(new User
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
            }, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            await OrganizationService.AddUserToOrganizationAsync(testUser.Id, organization.Id, OrganizationRoleEnum.Member, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            var currentTestUser = await CreateCurrentUserAsync(testUser.Email, [Roles.User]);

            // Act
            ApplicationErrorException? caught = null;

            try
            {
                await OrganizationService.UpdateOrganizationAsync(organization.Id, new Organization
                {
                    BaseRepositoryRole = BaseRepositoryRoleEnum.RepositoryWriter,
                    Name = "My Name",
                    LastEditedBy = currentTestUser.UserId,
                }, currentTestUser, default);
            }
            catch (ApplicationErrorException e)
            {
                caught = e;
            }

            // Assert
            Assert.IsNotNull(caught);
            Assert.AreEqual(ErrorCodes.EntityUnauthorized, caught.ErrorCode);
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
            }, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            // Act
            await OrganizationService.AddUserToOrganizationAsync(user.Id, organization.Id, OrganizationRoleEnum.Member, CurrentUser, default);
            await ProcessAllOutboxEventsAsync();

            await OrganizationService.RemoveUserFromOrganizationAsync(user.Id, organization.Id, OrganizationRoleEnum.Member, CurrentUser, default);
            await ProcessAllOutboxEventsAsync();

            // Assert
            var userOrganizationRoleTuples = await AclService.ReadTuplesAsync<Organization, User>(organization.Id, null, user.Id, null).ToListAsync();

            Assert.AreEqual(0, userOrganizationRoleTuples.Count);
        }

        /// <summary>
        /// Creates a new <see cref="Organization">, creates a new <see cref="User"/>, does NOT assign the user to the 
        /// organization, removes the user from the organization. Fails with an Exception.
        /// </summary>
        [TestMethod]
        public async Task RemoveUserOrganizationRoleAsync_UserNotAssignedToOrganization_FailsWithException()
        {
            // Arrange
            var user = await UserService.CreateUserAsync(new User
            {
                Email = "test@test.local",
                PreferredName = "Test User",
                LastEditedBy = CurrentUser.UserId
            }, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            var organization = await OrganizationService.CreateOrganizationAsync(new Organization
            {
                BaseRepositoryRole = BaseRepositoryRoleEnum.RepositoryReader,
                Name = "Unit Test",
                BillingAddress = "Billing Address",
                LastEditedBy = Users.GhostUserId
            }, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            // Act
            ApplicationErrorException? caught = null;
            try
            {
                await OrganizationService.RemoveUserFromOrganizationAsync(user.Id, organization.Id, OrganizationRoleEnum.Member, CurrentUser, default);
            }
            catch (ApplicationErrorException e)
            {
                caught = e;
            }

            // Assert
            Assert.IsNotNull(caught);
            Assert.AreEqual(ErrorCodes.UserNotAssignedToOrganization, caught.ErrorCode);
        }
    }
}
