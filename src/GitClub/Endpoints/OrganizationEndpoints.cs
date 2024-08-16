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
    public static class OrganizationEndpoints
    {
        private const string Tags = "organizations";

        public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/organizations/{organizationId}", GetOrganizationAsync)
                .WithName("GetOrganization")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapGet("/organizations", GetOrganizationsAsync)
                .WithName("GetOrganizations")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapPost("/organizations", PostOrganizationAsync)
                .WithName("PostOrganization")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapPut("/organizations/{organizationId}", PutOrganizationAsync)
                .WithName("PutOrganization")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapDelete("/organizations/{organizationId}", DeleteOrganizationAsync)
                .WithName("DeleteOrganization")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapGet("/organizations/{organizationId}/members", GetOrganizationMembersAsync)
                .WithName("GetMembers")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapGet("/organizations/{organizationId}/organization-roles/{role:OrganizationRoleEnum}/users", GetUserOrganizationRole)
                .WithName("GetUserOrganizationRoles")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapPut("/organizations/{organizationId}/organization-roles/users/{userId}/{role:OrganizationRoleEnum}", AddUserOrganizationRole)
                .WithName("AssignOrganizationRoleToUser")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapDelete("/organizations/{organizationId}/organization-roles/users/{userId}/{role:OrganizationRoleEnum}", DeleteUserOrganizationRole)
                .WithName("UnassignOrganizationRoleFromUser")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

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