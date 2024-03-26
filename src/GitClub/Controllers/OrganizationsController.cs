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
    public class OrganizationsController : ControllerBase
    {
        private readonly ILogger<OrganizationsController> _logger;

        private readonly ExceptionToApplicationErrorMapper _exceptionToApplicationErrorMapper;

        public OrganizationsController(ILogger<OrganizationsController> logger, ExceptionToApplicationErrorMapper exceptionToApplicationErrorMapper)
        {
            _logger = logger;
            _exceptionToApplicationErrorMapper = exceptionToApplicationErrorMapper;
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetOrganization([FromServices] OrganizationService organizationService, [FromServices] CurrentUser currentUser, [FromRoute(Name = "id")] int id, CancellationToken cancellationToken)
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

                var organization = await organizationService.GetOrganizationByIdAsync(id, currentUser, cancellationToken);

                return Ok(organization);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpGet("{id}/repositories")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetOrganizationRepositories([FromServices] RepositoryService repositoryService, [FromServices] CurrentUser currentUser, [FromRoute(Name = "id")] int id, CancellationToken cancellationToken)
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

                var repositories = await repositoryService.GetRepositoriesByOrganizationIdAsync(id, currentUser, cancellationToken);

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
        public async Task<IActionResult> GetOrganizationIssues([FromServices] IssueService issueService, [FromServices] CurrentUser currentUser, [FromRoute(Name = "id")] int id, CancellationToken cancellationToken)
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

                var issues = await issueService.GetIssuesByOrganizationIdAsync(id, currentUser, cancellationToken);

                return Ok(issues);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpGet]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetOrganizations([FromServices] OrganizationService organizationService, [FromServices] CurrentUser currentUser, CancellationToken cancellationToken)
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

                var organizations = await organizationService.GetOrganizationsAsync(currentUser, cancellationToken);

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
        public async Task<IActionResult> PostOrganization([FromServices] OrganizationService organizationService, [FromServices] CurrentUser currentUser, [FromBody] Organization Organization, CancellationToken cancellationToken)
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

                var organization = await organizationService.CreateOrganizationAsync(Organization, currentUser, cancellationToken);

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
        public async Task<IActionResult> PutOrganization([FromServices] OrganizationService organizationService, [FromServices] CurrentUser currentUser, [FromRoute(Name = "id")] int id, [FromBody] Organization Organization, CancellationToken cancellationToken)
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

                var organization = await organizationService.UpdateOrganizationAsync(id, Organization, currentUser, cancellationToken);

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
        public async Task<IActionResult> DeleteOrganization([FromServices] OrganizationService organizationService, [FromServices] CurrentUser currentUser, [FromRoute(Name = "id")] int key, CancellationToken cancellationToken)
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

                await organizationService.DeleteOrganizationAsync(key, currentUser, cancellationToken);

                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpGet("{organizationId}/members")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetUserOrganizationRoles(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "organizationId")] int organizationId,
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

                var userOrganizationRoles = await organizationService.GetUserOrganizationRolesByOrganizationIdAsync(organizationId, currentUser, cancellationToken);

                return Ok(userOrganizationRoles);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpGet("{organizationId}/organization-roles/{role:OrganizationRoleEnum}/users")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> GetUserOrganizationRoles(
            [FromServices] OrganizationService organizationService, 
            [FromServices] CurrentUser currentUser, 
            [FromRoute(Name = "organizationId")] int organizationId, 
            [FromRoute(Name = "role")] OrganizationRoleEnum role, 
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

                var userOrganizationRoles = await organizationService.GetUserOrganizationRolesByOrganizationIdAndRoleAsync(organizationId, role, currentUser, cancellationToken);

                return Ok(userOrganizationRoles);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpPut("{organizationId}/organization-roles/users/{userId}/{role:OrganizationRoleEnum}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> AddUserOrganizationRole(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "organizationId")] int organizationId,
            [FromRoute(Name = "userId")] int userId,
            [FromRoute(Name = "role")] OrganizationRoleEnum role,
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

                var userOrganizationRole = await organizationService
                    .AddUserOrganizationRoleAsync(organizationId, userId, role, currentUser, cancellationToken);

                return Ok(userOrganizationRole);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }

        [HttpDelete("{organizationId}/organization-roles/users/{userId}/{role:OrganizationRoleEnum}")]
        [Authorize(Policy = Policies.RequireUserRole)]
        [EnableRateLimiting(Policies.PerUserRatelimit)]
        public async Task<IActionResult> DeleteUserOrganizationRole(
            [FromServices] OrganizationService organizationService,
            [FromServices] CurrentUser currentUser,
            [FromRoute(Name = "organizationId")] int organizationId,
            [FromRoute(Name = "userId")] int userId,
            [FromRoute(Name = "role")] OrganizationRoleEnum role,
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

                await organizationService.RemoveUserOrganizationRoleAsync(organizationId, userId, role, currentUser, cancellationToken);

                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception exception)
            {
                return _exceptionToApplicationErrorMapper.CreateApplicationErrorResult(HttpContext, exception);
            }
        }
    }
}