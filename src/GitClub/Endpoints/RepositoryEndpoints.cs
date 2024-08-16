// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Errors;
using GitClub.Infrastructure.Exceptions;
using GitClub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GitClub.Endpoints
{
    public static partial class RepositoryEndpoints
    {
        private const string Tags = "gitclub";

        public static IEndpointRouteBuilder MapGitClubEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/repositories/{repositoryId}", GetRepositoryAsync)
                .WithName("GetRepository")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapGet("/repositories", GetRepositoriesAsync)
                .WithName("GetRepositories")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapPost("/repositories", PostRepositoryAsync)
                .WithName("PostRepository")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapPut("/repositories/{repositoryId}", PutRepositoryAsync)
                .WithName("PutRepository")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapDelete("/repositories/{repositoryId}", DeleteRepositoryAsync)
                .WithName("DeleteRepository")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapGet("/repositories/{repositoryId}/collaborators", GetCollaboratorsAsync)
                .WithName("GetUserRepositoryRoles")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapGet("/repositories/{repositoryId}/collaborators/{userId}/{role:RepositoryRoleEnum}", AddCollaboratorAsync)
                .WithName("GetUserRepositoryRoles")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapDelete("/repositories/{repositoryId}/collaborators/{userId}", DeleteCollaboratorAsync)
                .WithName("GetUserRepositoryRoles")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapGet("/repositories/{repositoryId}/teams", GetTeamsAsync)
                .WithName("GetUserRepositoryRoles")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapGet("/repositories/{repositoryId}/teams/{teamId}/{role:RepositoryRoleEnum}", AddTeamAsync)
                .WithName("GetUserRepositoryRoles")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapDelete("/repositories/{repositoryId}/teams/{teamId}", DeleteTeamAsync)
                .WithName("GetUserRepositoryRoles")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            return app;
        }

        public static async Task<IResult> GetRepositoryAsync(
            [FromServices] RepositoryService RepositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromServices] CancellationToken cancellationToken)
        {
            var repository = await RepositoryService.GetRepositoryByIdAsync(repositoryId, currentUser, cancellationToken);

            return TypedResults.Ok(repository);
        }

        public static async Task<IResult> GetRepositorysAsync(
            [FromServices] RepositoryService RepositoryService,
            [FromServices] CurrentUser currentUser,
            [FromServices] CancellationToken cancellationToken)
        {
            var repository = await RepositoryService.GetRepositoriesAsync(currentUser, cancellationToken);

            return TypedResults.Ok(repository);
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
    }
}