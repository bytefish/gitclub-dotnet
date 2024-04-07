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
    public class UserServiceTests : IntegrationTestBase
    {
        protected UserService UserService => GetRequiredService<UserService>();
        protected OrganizationService OrganizationService => GetRequiredService<OrganizationService>();
        protected TeamService TeamService => GetRequiredService<TeamService>();
        protected RepositoryService RepositoryService => GetRequiredService<RepositoryService>();
        protected IssueService IssueService => GetRequiredService<IssueService>();
        protected AclService AclService => GetRequiredService<AclService>();

        /// <summary>
        /// Creates a new <see cref="Organization"> and creates the OpenFGA Tuples.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task CreateUserAndAssignmentsAsync_Success()
        {
            // Arrange
            var organization = await OrganizationService.CreateOrganizationAsync(new Organization
            {
                BaseRepositoryRole = BaseRepositoryRoleEnum.RepositoryReader,
                Name = "Unit Test Organization",
                BillingAddress = "Billing Address",
                LastEditedBy = Users.GhostUserId
            }, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            var team = await TeamService.CreateTeamAsync(new Team
            {
                OrganizationId = organization.Id,
                Name = "Rockstar Unit Test Team",
                LastEditedBy = Users.GhostUserId
            }, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            var user = await UserService.CreateUserAsync(new User
            {
                Email = "test-user@test.local",
                PreferredName = "Unit Test",
                LastEditedBy = Users.GhostUserId
            }, CurrentUser, default);

            var repository = await RepositoryService.CreateRepositoryAsync(new Repository 
            {
                Name = "Unit Test Repository",
                OrganizationId = organization.Id,
                LastEditedBy = Users.GhostUserId
            }, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            var issue = await IssueService.CreateIssueAsync(new Issue
            {
                RepositoryId = repository.Id,
                Closed = false,
                Title = "Title Issue Test",
                Content = "Content Issue Test",
                CreatedBy = user.Id, 
                LastEditedBy = user.Id
            }, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            await OrganizationService
                .AddUserToOrganizationAsync(user.Id, organization.Id, OrganizationRoleEnum.Administrator, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            await TeamService
                .AddUserToTeamAsync(user.Id, team.Id, TeamRoleEnum.Maintainer, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            await RepositoryService
                .AddUserToRepositoryAsync(user.Id, repository.Id, RepositoryRoleEnum.Triager, CurrentUser, default);

            await ProcessAllOutboxEventsAsync();

            // Act
            await UserService.DeleteUserByUserIdAsync(user.Id, CurrentUser, default);
            await ProcessAllOutboxEventsAsync();

            // Assert
        }

    }
}
