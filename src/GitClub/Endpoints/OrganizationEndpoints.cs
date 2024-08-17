// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Errors;
using GitClub.Services;
using Microsoft.AspNetCore.Mvc;

namespace GitClub.Endpoints
{
    public static class OrganizationEndpoints
    {
        private const string Tags = "organizations";

        public static IEndpointRouteBuilder MapOrganizationsEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app
                .MapGroup("/organizations")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

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

            return app;
        }

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
    }
}