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
    public class TeamsController : ControllerBase
    {
        private readonly ILogger<TeamsController> _logger;

        private readonly ExceptionToApplicationErrorMapper _exceptionToApplicationErrorMapper;

        public TeamsController(ILogger<TeamsController> logger, ExceptionToApplicationErrorMapper exceptionToApplicationErrorMapper)
        {
            _logger = logger;
            _exceptionToApplicationErrorMapper = exceptionToApplicationErrorMapper;
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetTeam([FromServices] TeamService teamService, [FromRoute(Name = "id")] int id, CancellationToken cancellationToken)
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

                var team = await teamService.GetTeamByIdAsync(id, User.GetUserId(), cancellationToken);

                return Ok(team);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpGet]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetTeams([FromServices] TeamService teamService, CancellationToken cancellationToken)
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

                var teams = await teamService.GetTeamsByUserIdAsync(User.GetUserId(), cancellationToken);

                return Ok(teams);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpPost]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> PostTeam([FromServices] TeamService teamService, [FromBody] Team team, CancellationToken cancellationToken)
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

                var createdTeam = await teamService.CreateTeamAsync(team, User.GetUserId(), cancellationToken);

                return Ok(createdTeam);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> PutTeam([FromServices] TeamService teamService, [FromRoute(Name = "id")] int id, [FromBody] Team team, CancellationToken cancellationToken)
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

                var updatedTeam = await teamService.UpdateTeamAsync(id, team, User.GetUserId(), cancellationToken);

                return Ok(updatedTeam);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> DeleteTeam([FromServices] TeamService teamService, [FromRoute(Name = "id")] int key, CancellationToken cancellationToken)
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

                await teamService.DeleteTeamAsync(key, User.GetUserId(), cancellationToken);

                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }
    }
}