// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Errors;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using GitClub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GitClub.Controllers
{
    [Route("[controller]")]
    public class IssuesController : ControllerBase
    {
        private readonly ILogger<IssuesController> _logger;

        private readonly ExceptionToApplicationErrorMapper _exceptionToApplicationErrorMapper;

        public IssuesController(ILogger<IssuesController> logger, ExceptionToApplicationErrorMapper exceptionToApplicationErrorMapper)
        {
            _logger = logger;
            _exceptionToApplicationErrorMapper = exceptionToApplicationErrorMapper;
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetIssue([FromServices] IssueService issueService, [FromRoute(Name = "id")] int id, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            try
            {
                if (!ModelState.IsValid)
                {
                    throw new InvalidModelStateException
                    {
                        ModelStateDictionary = ModelState
                    };
                }

                var issue = await issueService.GetIssueByIdAsync(id, User.GetUserId(), cancellationToken);

                return Ok(issue);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpGet]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetIssues([FromServices] IssueService issueService, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            try
            {
                if (!ModelState.IsValid)
                {
                    throw new InvalidModelStateException
                    {
                        ModelStateDictionary = ModelState
                    };
                }

                var issues = await issueService.GetIssuesByUserIdAsync(User.GetUserId(), cancellationToken);

                return Ok(issues);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpPost]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> PostIssue([FromServices] IssueService issueService, [FromBody] Issue issue, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            try
            {
                if (!ModelState.IsValid)
                {
                    throw new InvalidModelStateException
                    {
                        ModelStateDictionary = ModelState
                    };
                }

                var createdIssue = await issueService.CreateIssueAsync(issue, User.GetUserId(), cancellationToken);

                return Ok(createdIssue);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> PutIssue([FromServices] IssueService issueService, [FromRoute(Name = "id")] int id, [FromBody] Issue issue, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            try
            {
                if (!ModelState.IsValid)
                {
                    throw new InvalidModelStateException
                    {
                        ModelStateDictionary = ModelState
                    };
                }

                var updatedIssue = await issueService.UpdateIssueAsync(id, issue, User.GetUserId(), cancellationToken);

                return Ok(updatedIssue);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> DeleteIssue([FromServices] IssueService issueService, [FromRoute(Name = "id")] int key, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            try
            {
                if (!ModelState.IsValid)
                {
                    throw new InvalidModelStateException
                    {
                        ModelStateDictionary = ModelState
                    };
                }

                await issueService.DeleteIssueAsync(key, User.GetUserId(), cancellationToken);

                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }
    }
}