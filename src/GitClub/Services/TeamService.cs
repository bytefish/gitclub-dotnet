// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Errors;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using OpenFga.Sdk.Model;

namespace GitClub.Services
{
    public class TeamService
    {
        private readonly ILogger<TeamService> _logger;

        private readonly ApplicationDbContext _applicationDbContext;
        private readonly AclService _aclService;

        public TeamService(ILogger<TeamService> logger, ApplicationDbContext applicationDbContext, AclService aclService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _aclService = aclService;
        }

        public async Task<Team> CreateTeamAsync(Team team, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // Make sure the Current User is the last editor:
            team.LastEditedBy = currentUserId;

            // Add the new Task, the HiLo Pattern automatically assigns a new Id using the HiLo Pattern
            await _applicationDbContext
                .AddAsync(team, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Add to ACL Store
            await _aclService
                .AddRelationshipAsync<Team, User>(team.Id, Relations.Owner, currentUserId, null)
                .ConfigureAwait(false);

            return team;
        }

        public async Task<Team> GetTeamByIdAsync(int teamId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                };
            }

            var team = await _applicationDbContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                };
            }

            return team;
        }

        public async Task<List<Team>> GetTeamsByOrganizationIdAsync(int organizationId, int userId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(userId, organizationId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                };
            }

            var teams = await _applicationDbContext.Teams
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return teams;
        }

        public async Task<List<Team>> GetTeamsByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var teams = await _aclService
                .ListUserObjectsAsync<Team>(userId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            return teams;
        }

        public async Task<Team> UpdateTeamAsync(int teamId, Team team, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Team),
                    EntityId = team.Id,
                };
            }

            bool isUpdateAuthorized = await _aclService
                .CheckUserObjectAsync(currentUserId, team, Actions.CanWrite, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = team.Id,
                    UserId = currentUserId,
                };
            }

            int rowsAffected = await _applicationDbContext.Teams
                .Where(t => t.Id == teamId && t.RowVersion == team.RowVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Name, team.Name)
                    .SetProperty(x => x.LastEditedBy, currentUserId), cancellationToken)
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new EntityConcurrencyException()
                {
                    EntityName = nameof(Team),
                    EntityId = team.Id,
                };
            }

            return team;
        }

        public async Task DeleteTeamAsync(int teamId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                };
            }

            var team = await _applicationDbContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                };
            }

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, Actions.CanWrite, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                    UserId = currentUserId,
                };
            }

            await _applicationDbContext.Teams
                    .Where(t => t.Id == team.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

            // TODO Delete all Tuples for the Team
        }

        public async Task<UserTeamRole> CreateUserTeamRoleAsync(UserTeamRole userTeamRole, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, userTeamRole.TeamId, Relations.Writer, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = userTeamRole.TeamId,
                    UserId = currentUserId,
                };
            }

            var userTeamRoleExists = await _applicationDbContext.UserTeamRoles
                .AnyAsync(x => x.UserId == userTeamRole.UserId && x.TeamId == userTeamRole.TeamId, cancellationToken)
                .ConfigureAwait(false);

            if(userTeamRoleExists)
            {

            }

            await _applicationDbContext
                .AddAsync(userTeamRole)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            await _aclService
                .AddRelationshipAsync<Team, User>(userTeamRole.TeamId, userTeamRole.Name, userTeamRole.UserId, null)
                .ConfigureAwait(false);

            return userTeamRole;
        }

        public async Task DeleteUserTeamRoleAsync(int teamId, int userId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, Relations.Writer, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                    UserId = currentUserId,
                };
            }

            var userTeamRole = await _applicationDbContext.UserTeamRoles
                .Where(x => x.TeamId == teamId && x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if(userTeamRole == null)
            {
                throw new UserNotAssignedToTeamException
                {
                    TeamId = teamId,
                    UserId = userId
                };
            }

            await _aclService
                .DeleteRelationshipAsync<Team, User>(teamId, userTeamRole.Name, userId, null, cancellationToken)
                .ConfigureAwait(false);
        }


    }
}
