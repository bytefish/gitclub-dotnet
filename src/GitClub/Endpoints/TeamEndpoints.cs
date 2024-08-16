// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Errors;
using GitClub.Services;
using Microsoft.AspNetCore.Mvc;

namespace GitClub.Endpoints
{
    public static partial class TeamEndpoints
    {
        private const string Tags = "gitclub";

        public static IEndpointRouteBuilder MapGitClubEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/teams/{teamId}", GetTeamAsync)
                .WithName("GetTeam")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapGet("/teams", GetTeamsAsync)
                .WithName("GetTeams")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();
            
            app.MapPost("/teams", PostTeamAsync)
                .WithName("PostTeam")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapPut("/teams/{teamId}", PutTeamAsync)
                .WithName("PutTeam")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapDelete("/teams/{teamId}", DeleteTeamAsync)
                .WithName("DeleteTeam")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            return app;
        }

        public static async Task<IResult> GetTeamAsync(
            [FromServices] TeamService teamService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "teamId")] int teamId,
            [FromServices] CancellationToken cancellationToken)
        {
            var team = await teamService.GetTeamByIdAsync(teamId, currentUser, cancellationToken);

            return TypedResults.Ok(team);
        }

        public static async Task<IResult> GetTeamsAsync(
            [FromServices] TeamService teamService,
            [FromServices] CurrentUser currentUser, 
            [FromServices] CancellationToken cancellationToken)
        {
            var teams = await teamService.GetTeamsAsync(currentUser, cancellationToken);

            return TypedResults.Ok(teams);
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
        
        public static async Task<IResult> DeleteTeamAsync(
            [FromServices] TeamService teamService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "teamId")] int teamId,
            [FromServices] CancellationToken cancellationToken)
        {
            await teamService.DeleteTeamAsync(teamId, currentUser, cancellationToken);

            return TypedResults.NoContent();
        }
    }
}