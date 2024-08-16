// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Errors;
using GitClub.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GitClub.Models;
using System.Text.RegularExpressions;

namespace GitClub.Endpoints
{
    public static class AuthEndpoints
    {
        private const string Tags = "auth";

        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes
                .MapGroup("/auth")
                .WithTags("Auth API");

            group.MapGet("login", LoginAsync)
                .WithName("Login")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            group.MapGet("logout", LogoutAsync)
                .WithName("Logout")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            return group;
        }

        public static async Task<IResult> LoginAsync(
            [FromServices] UserService userService,
            [FromServices] HttpContext httpContext,
            [FromBody] Credentials credentials,
            [FromServices] CancellationToken cancellationToken)
        {
            // Create a claims for each request scope
            var userClaims = await userService
                .GetClaimsAsync(credentials.Email, credentials.Roles, cancellationToken)
                .ConfigureAwait(false);

            // Create the ClaimsPrincipal
            var claimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // It's a valid ClaimsPrincipal, sign in
            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties { IsPersistent = credentials.RememberMe });

            return TypedResults.Ok();
        }

        public static async Task<IResult> LogoutAsync(
            [FromServices] HttpContext httpContext,
            [FromServices] CancellationToken cancellationToken)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return TypedResults.Ok();
        }

    }
}