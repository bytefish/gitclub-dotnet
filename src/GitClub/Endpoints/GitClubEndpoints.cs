// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Errors;
using GitClub.Services;
using Microsoft.AspNetCore.Mvc;

namespace GitClub.Endpoints
{
    public static class GitClubEndpoints
    {
        private const string Tags = "gitclub";

        public static IEndpointRouteBuilder MapGitClubEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app
                .MapGroup("organizations")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            // Organization
            group.MapGet("/{organizationId}", GetOrganizationAsync)
                .WithName("GetOrganization");

            group.MapGet("/", GetOrganizationsAsync)
                .WithName("GetOrganizations");

            group.MapPost("/", PostOrganizationAsync)
                .WithName("PostOrganization");

            group.MapPut("/{organizationId}", PutOrganizationAsync)
                .WithName("PutOrganization");

            group.MapDelete("/{organizationId}", DeleteOrganizationAsync)
                .WithName("DeleteOrganization");

            group.MapGet("/{organizationId}/members", GetOrganizationMembersAsync)
                .WithName("GetMembers");

            group.MapGet("/{organizationId}/organization-roles/{role:OrganizationRoleEnum}/users", GetUserOrganizationRole)
                .WithName("GetUserOrganizationRoles");

            group.MapPut("/{organizationId}/organization-roles/users/{userId}/{role:OrganizationRoleEnum}", AddUserOrganizationRole)
                .WithName("AssignOrganizationRoleToUser");

            group.MapDelete("/{organizationId}/organization-roles/users/{userId}/{role:OrganizationRoleEnum}", DeleteUserOrganizationRole)
                .WithName("UnassignOrganizationRoleFromUser");

            // Teams
            group.MapGet("/{organizationId}/teams/{teamId}", GetTeamAsync)
                .WithName("GetTeam");

            group.MapGet("/{organizationId}/teams", GetTeamsAsync)
                .WithName("GetTeams");

            group.MapPost("/{organizationId}/teams", PostTeamAsync)
                .WithName("PostTeam");

            group.MapPut("/{organizationId}/teams/{teamId}", PutTeamAsync)
                .WithName("PutTeam");

            group.MapDelete("/{organizationId}/teams/{teamId}", DeleteTeamAsync)
                .WithName("DeleteTeam");

            // Repositories
            group.MapGet("/{organizationId}/repositories/{repositoryId}", GetRepositoryAsync)
                .WithName("GetRepository");

            group.MapGet("/{organizationId}/repositories", GetRepositoriesAsync)
                .WithName("GetRepositories");

            group.MapPost("/{organizationId}/repositories", PostRepositoryAsync)
                .WithName("PostRepository");

            group.MapPut("/{organizationId}/repositories/{repositoryId}", PutRepositoryAsync)
                .WithName("PutRepository");

            group.MapDelete("/{organizationId}/repositories/{repositoryId}", DeleteRepositoryAsync)
                .WithName("DeleteRepository");

            group.MapGet("/{organizationId}/repositories/{repositoryId}/collaborators", GetCollaboratorsAsync)
                .WithName("GetUserRepositoryRoles");

            group.MapGet("/{organizationId}/repositories/{repositoryId}/collaborators/{userId}/{role:RepositoryRoleEnum}", AddCollaboratorAsync)
                .WithName("GetUserRepositoryRoles");

            group.MapDelete("/{organizationId}/repositories/{repositoryId}/collaborators/{userId}", DeleteCollaboratorAsync)
                .WithName("GetUserRepositoryRoles");

            group.MapGet("/{organizationId}/repositories/{repositoryId}/teams", GetTeamsAsync)
                .WithName("GetUserRepositoryRoles");

            group.MapGet("/{organizationId}/repositories/{repositoryId}/teams/{teamId}/{role:RepositoryRoleEnum}", AddTeamAsync)
                .WithName("GetUserRepositoryRoles");

            group.MapDelete("/{organizationId}/repositories/{repositoryId}/teams/{teamId}", DeleteTeamAsync)
                .WithName("GetUserRepositoryRoles");

            // Issues
            group.MapGet("/{organizationId}/repositories/{repositoryId}/issues/{issueId}", GetIssueAsync)
                .WithName("GetIssue");

            group.MapGet("/{organizationId}/repositories/{repositoryId}/issues", GetIssuesAsync)
                .WithName("GetIssues");

            group.MapPost("/{organizationId}/repositories/{repositoryId}/issues", PostIssueAsync)
                .WithName("PostIssue");

            group.MapPut("/{organizationId}/repositories/{repositoryId}/issues/{issueId}", PutIssueAsync)
                .WithName("PutIssue");

            group.MapDelete("/{organizationId}/repositories/{repositoryId}/issues/{issueId}", DeleteIssueAsync)
                .WithName("DeleteIssue");

            return app;
        }

        // Organization

        public static async Task<IResult> GetOrganizationAsync(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "organizationId")] int organizationId,
            [FromServices] CancellationToken cancellationToken)
        {
            var organization = await organizationService.GetOrganizationByIdAsync(organizationId, currentUser, cancellationToken);

            return TypedResults.Ok(organization);
        }

        public static async Task<IResult> GetOrganizationsAsync(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromServices] CancellationToken cancellationToken)
        {
            var organization = await organizationService.GetOrganizationsAsync(currentUser, cancellationToken);

            return TypedResults.Ok(organization);
        }

        public static async Task<IResult> PostOrganizationAsync(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromBody] Organization organization,
            [FromServices] CancellationToken cancellationToken)
        {
            var createdOrganization = await organizationService.CreateOrganizationAsync(organization, currentUser, cancellationToken);

            return TypedResults.Ok(createdOrganization);
        }

        public static async Task<IResult> PutOrganizationAsync(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "organizationId")] int organizationId,
            [FromBody] Organization organization,
            [FromServices] CancellationToken cancellationToken)
        {
            var updatedOrganization = await organizationService.UpdateOrganizationAsync(organizationId, organization, currentUser, cancellationToken);

            return TypedResults.Ok(updatedOrganization);
        }

        public static async Task<IResult> DeleteOrganizationAsync(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "organizationId")] int organizationId,
            [FromServices] CancellationToken cancellationToken)
        {
            await organizationService.DeleteOrganizationAsync(organizationId, currentUser, cancellationToken);

            return TypedResults.NoContent();
        }

        public static async Task<IResult> GetOrganizationMembersAsync(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "organizationId")] int organizationId,
            [FromServices] CancellationToken cancellationToken)
        {
            var userOrganizationRoles = await organizationService.GetUserOrganizationRolesByOrganizationIdAsync(organizationId, currentUser, cancellationToken);

            return TypedResults.Ok(userOrganizationRoles);
        }

        public static async Task<IResult> GetUserOrganizationRole(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "organizationId")] int organizationId,
            [FromRoute(Name = "role")] OrganizationRoleEnum role,
            [FromServices] CancellationToken cancellationToken)
        {
            var userOrganizationRoles = await organizationService.GetUserOrganizationRolesByOrganizationIdAndRoleAsync(organizationId, role, currentUser, cancellationToken);

            return TypedResults.Ok(userOrganizationRoles);
        }

        public static async Task<IResult> AddUserOrganizationRole(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "organizationId")] int organizationId,
            [FromRoute(Name = "userId")] int userId,
            [FromRoute(Name = "role")] OrganizationRoleEnum role,
            [FromServices] CancellationToken cancellationToken)
        {
            var userOrganizationRole = await organizationService
                .AddUserToOrganizationAsync(userId, organizationId, role, currentUser, cancellationToken);

            return TypedResults.Ok(userOrganizationRole);
        }

        public static async Task<IResult> DeleteUserOrganizationRole(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "organizationId")] int organizationId,
            [FromRoute(Name = "userId")] int userId,
            [FromRoute(Name = "role")] OrganizationRoleEnum role,
            CancellationToken cancellationToken)
        {
            await organizationService.RemoveUserFromOrganizationAsync(userId, organizationId, role, currentUser, cancellationToken);

            return TypedResults.NoContent();
        }

        // Team
        public static async Task<IResult> GetTeamAsync(
            [FromServices] TeamService teamService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "teamId")] int teamId,
            [FromServices] CancellationToken cancellationToken)
        {
            var team = await teamService.GetTeamByIdAsync(teamId, currentUser, cancellationToken);

            return TypedResults.Ok(team);
        }


        public static async Task<IResult> PostTeamAsync(
            [FromServices] TeamService teamService,
            [FromServices] CurrentUser currentUser,
            [FromBody] Team team,
            [FromServices] CancellationToken cancellationToken)
        {
            var createdTeam = await teamService.CreateTeamAsync(team, currentUser, cancellationToken);

            return TypedResults.Ok(createdTeam);
        }

        public static async Task<IResult> PutTeamAsync(
            [FromServices] TeamService teamService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "teamId")] int teamId,
            [FromBody] Team team,
            [FromServices] CancellationToken cancellationToken)
        {
            var updatedTeam = await teamService.UpdateTeamAsync(teamId, team, currentUser, cancellationToken);

            return TypedResults.Ok(updatedTeam);
        }

        // Repository

        public static async Task<IResult> GetRepositoryAsync(
            [FromServices] RepositoryService RepositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromServices] CancellationToken cancellationToken)
        {
            var repository = await RepositoryService.GetRepositoryByIdAsync(repositoryId, currentUser, cancellationToken);

            return TypedResults.Ok(repository);
        }

        public static async Task<IResult> GetRepositoriesAsync(
            [FromServices] RepositoryService RepositoryService,
            [FromServices] CurrentUser currentUser,
            [FromServices] CancellationToken cancellationToken)
        {
            var repositories = await RepositoryService.GetRepositoriesAsync(currentUser, cancellationToken);

            return TypedResults.Ok(repositories);
        }

        public static async Task<IResult> PostRepositoryAsync(
            [FromServices] RepositoryService RepositoryService,
            [FromServices] CurrentUser currentUser,
            [FromBody] Repository repository,
            [FromServices] CancellationToken cancellationToken)
        {
            var createdRepository = await RepositoryService.CreateRepositoryAsync(repository, currentUser, cancellationToken);

            return TypedResults.Ok(createdRepository);
        }

        public static async Task<IResult> PutRepositoryAsync(
            [FromServices] RepositoryService RepositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromBody] Repository repository,
            [FromServices] CancellationToken cancellationToken)
        {
            var updatedRepository = await RepositoryService.UpdateRepositoryAsync(repositoryId, repository, currentUser, cancellationToken);

            return TypedResults.Ok(updatedRepository);
        }

        public static async Task<IResult> DeleteRepositoryAsync(
            [FromServices] RepositoryService RepositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromServices] CancellationToken cancellationToken)
        {
            await RepositoryService.DeleteRepositoryAsync(repositoryId, currentUser, cancellationToken);

            return TypedResults.NoContent();
        }

        public static async Task<IResult> GetCollaboratorsAsync(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromServices] CancellationToken cancellationToken)
        {
            var userRepositoryRoles = await repositoryService.GetUserRepositoryRolesByRepositoryIdAsync(repositoryId, currentUser, cancellationToken);

            return TypedResults.Ok(userRepositoryRoles);
        }

        public static async Task<IResult> AddCollaboratorAsync(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromRoute(Name = "userId")] int userId,
            [FromRoute(Name = "role")] RepositoryRoleEnum role,
            [FromServices] CancellationToken cancellationToken)
        {
            var userRepositoryRole = await repositoryService
                .AddUserToRepositoryAsync(userId, repositoryId, role, currentUser, cancellationToken);

            return TypedResults.Ok(userRepositoryRole);
        }

        public static async Task<IResult> DeleteCollaboratorAsync(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromRoute(Name = "userId")] int userId,
            [FromServices] CancellationToken cancellationToken)
        {
            await repositoryService.RemoveUserFromRepositoryAsync(userId, repositoryId, currentUser, cancellationToken);

            return TypedResults.NoContent();
        }

        public static async Task<IResult> GetTeamsAsync(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromServices] CancellationToken cancellationToken)
        {
            var teamRepositoryRoles = await repositoryService.GetTeamRepositoryRolesByRepositoryIdAsync(repositoryId, currentUser, cancellationToken);

            return TypedResults.Ok(teamRepositoryRoles);
        }

        public static async Task<IResult> AddTeamAsync(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromRoute(Name = "teamId")] int teamId,
            [FromRoute(Name = "role")] RepositoryRoleEnum role,
         CancellationToken cancellationToken)
        {
            var teamRepositoryRole = await repositoryService
                .AddTeamToRepositoryAsync(repositoryId, teamId, role, currentUser, cancellationToken);

            return TypedResults.Ok(teamRepositoryRole);
        }

        public static async Task<IResult> DeleteTeamAsync(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromRoute(Name = "teamId")] int teamId,
            [FromServices] CancellationToken cancellationToken)
        {
            await repositoryService.RemoveTeamFromRepositoryAsync(repositoryId, teamId, currentUser, cancellationToken);

            return TypedResults.NoContent();
        }

        // Issue

        public static async Task<IResult> GetIssueAsync(
            [FromServices] IssueService issueService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "issueId")] int issueId,
            [FromServices] CancellationToken cancellationToken)
        {
            var issue = await issueService.GetIssueByIdAsync(issueId, currentUser, cancellationToken);

            return TypedResults.Ok(issue);
        }

        public static async Task<IResult> GetIssuesAsync(
            [FromServices] IssueService issueService,
            [FromServices] CurrentUser currentUser,
            [FromServices] CancellationToken cancellationToken)
        {
            var issues = await issueService.GetIssuesAsync(currentUser, cancellationToken);

            return TypedResults.Ok(issues);
        }

        public static async Task<IResult> PostIssueAsync(
            [FromServices] IssueService issueService,
            [FromServices] CurrentUser currentUser, Issue issue,
            [FromServices] CancellationToken cancellationToken)
        {
            var createdIssue = await issueService.CreateIssueAsync(issue, currentUser, cancellationToken);

            return TypedResults.Ok(createdIssue);
        }

        public static async Task<IResult> PutIssueAsync(
            [FromServices] IssueService issueService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "issueId")] int issueId,
            [FromBody] Issue issue,
            [FromServices] CancellationToken cancellationToken)
        {
            var createdIssue = await issueService.UpdateIssueAsync(issueId, issue, currentUser, cancellationToken);

            return TypedResults.Ok(createdIssue);
        }

        public static async Task<IResult> DeleteIssueAsync(
            [FromServices] IssueService issueService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "issueId")] int issueId,
            [FromBody] Issue issue,
            [FromServices] CancellationToken cancellationToken)
        {
            await issueService.DeleteIssueAsync(issueId, currentUser, cancellationToken);

            return TypedResults.NoContent();
        }
    }
}