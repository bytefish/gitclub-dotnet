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
        private const string Tags = "teams";

        public static IEndpointRouteBuilder MapTeamsEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app
                .MapGroup("/teams")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            group.MapGet("/{teamId}", GetTeamAsync)
                .WithName("GetTeam");

            group.MapGet("/", GetTeamsAsync)
                .WithName("GetTeams");

            group.MapPost("/", PostTeamAsync)
                .WithName("PostTeam");

            group.MapPut("/{teamId}", PutTeamAsync)
                .WithName("PutTeam");

            group.MapDelete("/{teamId}", DeleteTeamAsync)
                .WithName("DeleteTeam");

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