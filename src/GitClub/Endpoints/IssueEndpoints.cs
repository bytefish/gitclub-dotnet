// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Errors;
using GitClub.Services;
using Microsoft.AspNetCore.Mvc;

namespace GitClub.Endpoints
{
    public static class IssueEndpoints
    {
        private const string Tags = "issues";

        public static IEndpointRouteBuilder MapGitClubEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/issues/{issueId}", GetIssueAsync)
                .WithName("GetIssue")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapGet("/issues", GetIssuesAsync)
                .WithName("GetIssues")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();
            
            app.MapPost("/issues", PostIssueAsync)
                .WithName("PostIssue")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapPut("/issues/{issueId}", PutIssueAsync)
                .WithName("PutIssue")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();

            app.MapDelete("/issues/{issueId}", DeleteIssueAsync)
                .WithName("DeleteIssue")
                .WithTags(Tags)
                .WithOpenApi()
                .AddEndpointFilter<ApplicationErrorExceptionFilter>();


            return app;
        }

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