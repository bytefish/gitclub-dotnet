// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Services;
using GitClub.Infrastructure;
using GitClub.Infrastructure.Constants;

namespace GitClub.Tests.Services
{
    [TestClass]
    public class OrganizationServiceTests : IntegrationTestBase
    {
        protected AclService AclService => GetRequiredService<AclService>();

        protected OrganizationService OrganizationService => GetRequiredService<OrganizationService>();

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
    }
}
