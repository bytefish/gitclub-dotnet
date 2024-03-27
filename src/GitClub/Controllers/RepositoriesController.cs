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
    public class RepositoriesController : ControllerBase
    {
        private readonly ILogger<RepositoriesController> _logger;

        private readonly ExceptionToApplicationErrorMapper _exceptionToApplicationErrorMapper;

        public RepositoriesController(ILogger<RepositoriesController> logger, ExceptionToApplicationErrorMapper exceptionToApplicationErrorMapper)
        {
            _logger = logger;
            _exceptionToApplicationErrorMapper = exceptionToApplicationErrorMapper;
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetRepository([FromServices] RepositoryService repositoryService, [FromServices] CurrentUser currentUser, [FromRoute(Name = "id")] int id, CancellationToken cancellationToken)
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

                var repository = await repositoryService.GetRepositoryByIdAsync(id, currentUser, cancellationToken);

                return Ok(repository);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpGet]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetRepositories([FromServices] RepositoryService repositoryService, [FromServices] CurrentUser currentUser, CancellationToken cancellationToken)
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

                var repositories = await repositoryService.GetRepositoriesAsync(currentUser, cancellationToken);

                return Ok(repositories);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpGet("{id}/issues")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetRepositoryIssues([FromServices] IssueService issueService, [FromServices] CurrentUser currentUser, [FromRoute(Name = "id")] int id, CancellationToken cancellationToken)
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

                var organizations = await issueService.GetIssuesByRepositoryIdAsync(id, currentUser, cancellationToken);

                return Ok(organizations);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpPost]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> PostRepository([FromServices] RepositoryService repositoryService, [FromServices] CurrentUser currentUser, [FromBody] Repository Repository, CancellationToken cancellationToken)
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

                var organization = await repositoryService.CreateRepositoryAsync(Repository, currentUser, cancellationToken);

                return Ok(organization);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> PutRepository([FromServices] RepositoryService repositoryService, [FromServices] CurrentUser currentUser, [FromRoute(Name = "id")] int id, [FromBody] Repository Repository, CancellationToken cancellationToken)
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

                var organization = await repositoryService.UpdateRepositoryAsync(id, Repository, currentUser, cancellationToken);

                return Ok(organization);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> DeleteRepository([FromServices] RepositoryService repositoryService, [FromServices] CurrentUser currentUser, [FromRoute(Name = "id")] int key, CancellationToken cancellationToken)
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

                await repositoryService.DeleteRepositoryAsync(key, currentUser, cancellationToken);

                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpGet("{repositoryId}/collaborators")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetCollaborators(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            CancellationToken cancellationToken)
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

                var userRepositoryRoles = await repositoryService.GetUserRepositoryRolesByRepositoryIdAsync(repositoryId, currentUser, cancellationToken);

                return Ok(userRepositoryRoles);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpPut("{repositoryId}/collaborators/{userId}/{role:RepositoryRoleEnum}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> AddCollaborator(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromRoute(Name = "userId")] int userId,
            [FromRoute(Name = "role")] RepositoryRoleEnum role,
            CancellationToken cancellationToken)
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

                var userRepositoryRole = await repositoryService
                    .AddUserToRepositoryAsync(userId, repositoryId, role, currentUser, cancellationToken);

                return Ok(userRepositoryRole);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpDelete("{repositoryId}/collaborators/{userId}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> DeleteCollaborator(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromRoute(Name = "userId")] int userId,
            CancellationToken cancellationToken)
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

                await repositoryService.RemoveUserFromRepositoryAsync(userId, repositoryId, currentUser, cancellationToken);

                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpGet("{repositoryId}/teams")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetTeams(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            CancellationToken cancellationToken)
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

                var teamRepositoryRoles = await repositoryService.GetTeamRepositoryRolesByRepositoryIdAsync(repositoryId, currentUser, cancellationToken);

                return Ok(teamRepositoryRoles);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpPut("{repositoryId}/teams/{teamId}/{role:RepositoryRoleEnum}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> AddTeam(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromRoute(Name = "teamId")] int teamId,
            [FromRoute(Name = "role")] RepositoryRoleEnum role,
            CancellationToken cancellationToken)
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

                var teamRepositoryRole = await repositoryService
                    .AddTeamToRepositoryAsync(repositoryId, teamId, role, currentUser, cancellationToken);

                return Ok(teamRepositoryRole);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpDelete("{repositoryId}/teams/{teamId}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> DeleteTeam(
            [FromServices] RepositoryService repositoryService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "repositoryId")] int repositoryId,
            [FromRoute(Name = "teamId")] int userId,
            CancellationToken cancellationToken)
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

                await repositoryService.RemoveTeamFromRepositoryAsync(repositoryId, userId, currentUser, cancellationToken);

                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

    }
}