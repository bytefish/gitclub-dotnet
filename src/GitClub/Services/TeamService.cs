// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.OpenFga;
using GitClub.Models;
using Microsoft.EntityFrameworkCore;

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

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUserId, team.OrganizationId, OrganizationRoleEnum.Owner, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Organization),
                    EntityId = team.OrganizationId,
                    UserId = currentUserId
                };
            }

            // Make sure the Current User is the last editor:
            team.LastEditedBy = currentUserId;

            // Add the new Team
            await _applicationDbContext
                .AddAsync(team, cancellationToken)
                .ConfigureAwait(false);

            // The creator is also the Maintainer
            var userTeamRole = new UserTeamRole
            {
                TeamId = team.Id,
                UserId = currentUserId,
                Role = TeamRoleEnum.Maintainer,
                LastEditedBy = currentUserId
            };

            await _applicationDbContext
                .AddAsync(userTeamRole, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Add Relations to Zanzibar
            var tuplesToWrite = new[]
            {
                RelationTuples.Create<Team, User>(team.Id, currentUserId, TeamRoleEnum.Maintainer)
            };

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);

            return team;
        }

        public async Task<Team> GetTeamByIdAsync(int teamId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, RepositoryRoleEnum.Reader, cancellationToken)
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
                .CheckUserObjectAsync<Organization>(userId, organizationId, OrganizationRoleEnum.Member, cancellationToken)
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
                .ListUserObjectsAsync<Team>(userId, TeamRoleEnum.Member.AsRelation(), cancellationToken)
                .ConfigureAwait(false);

            return teams;
        }

        public async Task<Team> UpdateTeamAsync(int teamId, Team values, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, TeamRoleEnum.Member, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                };
            }

            bool isUpdateAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, TeamRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isUpdateAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                    UserId = currentUserId,
                };
            }

            var original = await _applicationDbContext.Teams.AsNoTracking()
                .Where(x => x.Id == teamId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if(original == null)
            {
                throw new EntityNotFoundException 
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                };
            }

            int rowsAffected = await _applicationDbContext.Teams
                .Where(t => t.Id == teamId && t.RowVersion == values.RowVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Name, values.Name)
                    .SetProperty(x => x.LastEditedBy, currentUserId), cancellationToken)
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new EntityConcurrencyException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                };
            }

            var updated = await _applicationDbContext.Teams.AsNoTracking()
                .Where(x => x.Id == teamId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (updated == null)
            {
                throw new EntityNotFoundException
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                };
            }

            return updated;
        }

        public async Task DeleteTeamAsync(int teamId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, TeamRoleEnum.Member, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                };
            }

            var team = await _applicationDbContext.Teams.AsNoTracking()
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

            bool isWriteAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, TeamRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isWriteAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                    UserId = currentUserId,
                };
            }

            var userTeamRoles = await _applicationDbContext.UserTeamRoles.AsNoTracking()
                .Where(x => x.TeamId == teamId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext.UserTeamRoles
                .Where(t => t.Id == team.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext.Teams
                    .Where(t => t.Id == team.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

            // Delete tuples in Zanzibar
            List<RelationTuple> tuplesToDelete = [];

            foreach (var userTeamRole in userTeamRoles)
            {
                var tuple = RelationTuples.Create<Team, User>(userTeamRole.TeamId, userTeamRole.UserId, userTeamRole.Role);

                tuplesToDelete.Add(tuple);
            }

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken) 
                .ConfigureAwait(false);
        }

        public async Task<UserTeamRole> AssignUserToTeamAsync(int userId, int teamId, TeamRoleEnum role, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, TeamRoleEnum.Member, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId
                };
            }

            bool isWriteAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, TeamRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isWriteAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                    UserId = currentUserId,
                };
            }

            var userTeamRoleExists = await _applicationDbContext.UserTeamRoles
                .AnyAsync(x => x.UserId == userId && x.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            if(userTeamRoleExists)
            {
                throw new UserAlreadyAssignedToTeamException 
                { 
                    TeamId = teamId, 
                    UserId = userId 
                };
            }

            var userTeamRole = new UserTeamRole 
            {
                UserId = userId,
                TeamId = teamId,
                Role = role,
                LastEditedBy = currentUserId
            };

            await _applicationDbContext
                .AddAsync(userTeamRole)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Write tuples to Zanzibar
            var tuplesToWrite = new[]
            {
                RelationTuples.Create<Team, User>(userTeamRole.TeamId, userTeamRole.UserId, userTeamRole.Role, null)
            };

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);

            return userTeamRole;
        }

        public async Task DeleteUserFromTeamAsync(int userId, int teamId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, TeamRoleEnum.Member, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId
                };
            }

            bool isWriteAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUserId, teamId, TeamRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isWriteAuthorized)
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

            // Delete Tuples from Zanzibar
            var tuplesToDelete = new[]
            {
                RelationTuples.Create<Team, User>(userTeamRole.TeamId, userTeamRole.UserId, userTeamRole.Role, null)
            };

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
