// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.OpenFga;
using GitClub.Infrastructure.Outbox;
using GitClub.Infrastructure.Outbox.Messages;
using Microsoft.EntityFrameworkCore;

namespace GitClub.Services
{
    public class TeamService
    {
        private readonly ILogger<TeamService> _logger;

        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly AclService _aclService;

        public TeamService(ILogger<TeamService> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, AclService aclService)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _aclService = aclService;
        }

        public async Task<Team> CreateTeamAsync(Team team, CurrentUser currentUser, CancellationToken cancellationToken)
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

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            // Make sure the Current User is the last editor:
            team.LastEditedBy = currentUser.UserId;

            // Add the new Team
            await applicationDbContext
                .AddAsync(team, cancellationToken)
                .ConfigureAwait(false);

            // The creator is also the Maintainer
            var userTeamRole = new UserTeamRole
            {
                TeamId = team.Id,
                UserId = currentUser.UserId,
                Role = TeamRoleEnum.Maintainer,
                LastEditedBy = currentUser.UserId
            };

            await applicationDbContext
                .AddAsync(userTeamRole, cancellationToken)
                .ConfigureAwait(false);

            var outboxEvent = OutboxEventUtils.Create(new TeamCreatedMessage
            {
                OrganizationId = team.OrganizationId,
                TeamId = team.Id,
                UserTeamRoles = new[] { userTeamRole }
                        .Select(x => new AddedUserToTeamMessage
                        {
                            TeamId = team.Id,
                            UserId = currentUser.UserId,
                            Role = TeamRoleEnum.Maintainer
                        })
                        .ToList()
            }, lastEditedBy: currentUser.UserId);

            await applicationDbContext
                .AddAsync(outboxEvent, cancellationToken)
                .ConfigureAwait(false);

            await applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return team;
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

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var team = await applicationDbContext.Teams
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

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var teams = await applicationDbContext.Teams
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return teams;
        }

        public async Task<List<Team>> GetTeamsAsync(CurrentUser user, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var teams = await _aclService
                .ListUserObjectsAsync<Team>(applicationDbContext, user.UserId, TeamRoleEnum.Member.AsRelation(), cancellationToken)
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

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var original = await applicationDbContext.Teams.AsNoTracking()
                .Where(x => x.Id == teamId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (original == null)
            {
                throw new EntityNotFoundException
                {
                    EntityName = nameof(Team),
                    EntityId = teamId,
                };
            }

            using (var transaction = await applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                int rowsAffected = await applicationDbContext.Teams
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

                var outboxEvent = OutboxEventUtils.Create(new TeamUpdatedMessage { TeamId = teamId }, lastEditedBy: currentUser.UserId);

                await applicationDbContext.OutboxEvents
                    .AddAsync(outboxEvent, cancellationToken)
                    .ConfigureAwait(false);

                await applicationDbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            var updated = await applicationDbContext.Teams.AsNoTracking()
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

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var team = await applicationDbContext.Teams.AsNoTracking()
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

            using (var transaction = await applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                var userTeamRoles = await applicationDbContext.UserTeamRoles.AsNoTracking()
                    .Where(x => x.TeamId == teamId)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                await applicationDbContext.UserTeamRoles
                    .Where(t => t.Id == team.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

                await applicationDbContext.Teams
                        .Where(t => t.Id == team.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);

                // Write Outbox Event at the same time
                var outboxEvent = OutboxEventUtils.Create(new TeamDeletedMessage
                {
                    TeamId = team.Id,
                    UserTeamRoles = userTeamRoles
                        .Select(x => new RemovedUserFromTeamMessage { TeamId = x.TeamId, UserId = x.UserId, Role = x.Role })
                        .ToList()
                }, lastEditedBy: currentUser.UserId);

                await applicationDbContext
                    .AddAsync(outboxEvent, cancellationToken)
                    .ConfigureAwait(false);

                await applicationDbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
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

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var userTeamRoles = await applicationDbContext.UserTeamRoles
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

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var userTeamRoleExists = await applicationDbContext.UserTeamRoles
                .AnyAsync(x => x.UserId == userId && x.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (userTeamRoleExists)
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

            await applicationDbContext
                .AddAsync(userTeamRole, cancellationToken)
                .ConfigureAwait(false);

            var outboxEvent = OutboxEventUtils.Create(new AddedUserToTeamMessage
            {
                Role = userTeamRole.Role,
                TeamId = userTeamRole.TeamId,
                UserId = userTeamRole.UserId
            }, lastEditedBy: currentUser.UserId);

            await applicationDbContext
                .AddAsync(outboxEvent, cancellationToken)
                .ConfigureAwait(false);

            await applicationDbContext
                .SaveChangesAsync(cancellationToken)
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

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var userTeamRole = await applicationDbContext.UserTeamRoles.AsNoTracking()
                .Where(x => x.TeamId == teamId && x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (userTeamRole == null)
            {
                throw new UserNotAssignedToTeamException
                {
                    TeamId = teamId,
                    UserId = userId
                };
            }

            using (var transaction = await applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                await applicationDbContext.UserTeamRoles
                    .Where(x => x.Id == userTeamRole.Id)
                    .ExecuteDeleteAsync()
                    .ConfigureAwait(false);

                var outboxEvent = OutboxEventUtils.Create(new RemovedUserFromTeamMessage
                {
                    TeamId = userTeamRole.TeamId,
                    UserId = userTeamRole.UserId,
                    Role = userTeamRole.Role
                }, lastEditedBy: currentUser.UserId);

                await applicationDbContext.OutboxEvents
                    .AddAsync(outboxEvent, cancellationToken)
                    .ConfigureAwait(false);

                await applicationDbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
