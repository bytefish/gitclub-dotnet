// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.OpenFga;
using GitClub.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GitClub.Services
{
    public class OutboxService
    {
        private readonly ILogger<OutboxService> _logger;

        private readonly ApplicationDbContext _applicationDbContext;
        private readonly AclService _aclService;

        public OutboxService(ILogger<OutboxService> logger, ApplicationDbContext applicationDbContext, AclService aclService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _aclService = aclService;
        }

        public async Task<OutboxEvent> CreateOutboxEventAsync<TEvent>(TEvent payload, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUser.UserId, team.OrganizationId, OrganizationRoleEnum.Owner, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Organization),
                    EntityId = team.OrganizationId,
                    UserId = currentUser.UserId
                };
            }

            var outboxEvent = new OutboxEvent
            {
                EventSource = "",
                EventType = typeof(TEvent).FullName!,
                EventTime = DateTimeOffset.UtcNow,
                Payload = JsonSerializer.SerializeToDocument(payload),
                LastEditedBy = currentUser.UserId
            };

            // Add the new Team
            await _applicationDbContext
                .AddAsync(outboxEvent, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Add Relations to Zanzibar
            var tuplesToWrite = new[]
            {
                RelationTuples.Create<Team, Organization>(team.Id, currentUser.UserId, TeamRoleEnum.Owner),
                RelationTuples.Create<Team, User>(team.Id, currentUser.UserId, TeamRoleEnum.Maintainer)
            };

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);

            return outboxEvent;
        }

        public async Task<Team> GetTeamByIdAsync(int teamId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, RepositoryRoleEnum.Reader, cancellationToken)
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

        public async Task<List<Team>> GetTeamsAsync(CurrentUser user, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var teams = await _aclService
                .ListUserObjectsAsync<Team>(user.UserId, TeamRoleEnum.Member.AsRelation(), cancellationToken)
                .ConfigureAwait(false);

            return teams;
        }

        public async Task<Team> UpdateTeamAsync(int teamId, Team values, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, TeamRoleEnum.Member, cancellationToken)
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
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, TeamRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isUpdateAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                    UserId = currentUser.UserId,
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
                    .SetProperty(x => x.LastEditedBy, currentUser.UserId), cancellationToken)
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

        public async Task DeleteTeamAsync(int teamId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, TeamRoleEnum.Member, cancellationToken)
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
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, TeamRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isWriteAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                    UserId = currentUser.UserId,
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

        public async Task<List<UserTeamRole>> GetUserTeamRolesByTeamIdAsync(int teamId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, TeamRoleEnum.Member, cancellationToken)
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
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, TeamRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isWriteAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                    UserId = currentUser.UserId,
                };
            }

            var userTeamRoles = await _applicationDbContext.UserTeamRoles
                .Where(x => x.TeamId == teamId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return userTeamRoles;
        }

        public async Task<UserTeamRole> AddUserToTeamAsync(int userId, int teamId, TeamRoleEnum role, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, TeamRoleEnum.Member, cancellationToken)
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
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, TeamRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isWriteAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                    UserId = currentUser.UserId,
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
                LastEditedBy = currentUser.UserId
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

        public async Task RemoveUserFromTeamAsync(int userId, int teamId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, TeamRoleEnum.Member, cancellationToken)
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
                .CheckUserObjectAsync<Team>(currentUser.UserId, teamId, TeamRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isWriteAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                    UserId = currentUser.UserId,
                };
            }

            var userTeamRole = await _applicationDbContext.UserTeamRoles.AsNoTracking()
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
