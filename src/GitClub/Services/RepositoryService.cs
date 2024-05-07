// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Messages;
using GitClub.Infrastructure.OpenFga;
using GitClub.Infrastructure.Outbox;
using GitClub.Infrastructure.Outbox.Messages;
using GitClub.Models;
using Microsoft.EntityFrameworkCore;

namespace GitClub.Services
{
    public class RepositoryService
    {
        private readonly ILogger<RepositoryService> _logger;

        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly AclService _aclService;

        public RepositoryService(ILogger<RepositoryService> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, AclService aclService)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _aclService = aclService;
        }

        public async Task<Repository> CreateRepositoryAsync(Repository repository, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUser.UserId, repository.OrganizationId, OrganizationRoleEnum.Member, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException
                {
                    EntityName = nameof(Organization),
                    EntityId = repository.OrganizationId,
                    UserId = currentUser.UserId
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            // Make sure the Current User is the editor:
            repository.LastEditedBy = currentUser.UserId;

            await applicationDbContext
                .AddAsync(repository, cancellationToken)
                .ConfigureAwait(false);

            // The User, that created the Repository is also the Administrator of the Repository.
            var userRepositoryRole = new UserRepositoryRole
            {
                RepositoryId = repository.Id,
                UserId = currentUser.UserId,
                Role = RepositoryRoleEnum.Administrator,
                LastEditedBy = currentUser.UserId
            };

            await applicationDbContext
                .AddAsync(userRepositoryRole, cancellationToken)
                .ConfigureAwait(false);

            var outboxEvent = OutboxEventUtils.Create(new RepositoryCreatedMessage
            {
                RepositoryId = repository.Id,
                OrganizationId = repository.OrganizationId,
                UserRepositoryRoles = new[] { userRepositoryRole }
                    .Select(x => new AddedUserToRepositoryMessage 
                    { 
                        RepositoryId = x.RepositoryId,
                        UserId = x.UserId,
                        Role = x.Role
                    })
                    .ToList()
            }, lastEditedBy: currentUser.UserId);

            await applicationDbContext
                .AddAsync(outboxEvent, cancellationToken)
                .ConfigureAwait(false);

            await applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return repository;
        }

        public async Task<Repository> GetRepositoryByIdAsync(int repositoryId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var repository = await applicationDbContext.Repositories.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == repositoryId, cancellationToken)
                .ConfigureAwait(false);

            if (repository == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                };
            }

            return repository;
        }

        public async Task<List<Repository>> GetRepositoriesByOrganizationIdAsync(int organizationId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUser.UserId, organizationId, OrganizationRoleEnum.Member, cancellationToken)
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

            var repositories = await applicationDbContext.Repositories.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId)
                .ToListAsync()
                .ConfigureAwait(false);

            return repositories;
        }

        public async Task<List<Repository>> GetRepositoriesAsync(CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var repositories = await _aclService
                .ListUserObjectsAsync<Repository>(applicationDbContext, currentUser.UserId, RepositoryRoleEnum.Reader.AsRelation(), cancellationToken)
                .ConfigureAwait(false);

            return repositories;
        }

        public async Task<Repository> UpdateRepositoryAsync(int repositoryId, Repository values, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = values.Id,
                };
            }

            bool isUpdateAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Writer, cancellationToken)
                .ConfigureAwait(false);

            if (!isUpdateAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = values.Id,
                    UserId = currentUser.UserId,
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            using (var transaction = await applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                int rowsAffected = await applicationDbContext.Repositories
                    .Where(t => t.Id == repositoryId && t.RowVersion == values.RowVersion)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Name, values.Name)
                        .SetProperty(x => x.OrganizationId, values.OrganizationId)
                        .SetProperty(x => x.LastEditedBy, currentUser.UserId), cancellationToken)
                    .ConfigureAwait(false);

                if (rowsAffected == 0)
                {
                    throw new EntityConcurrencyException()
                    {
                        EntityName = nameof(Repository),
                        EntityId = values.Id,
                    };
                }

                var outboxEvent = OutboxEventUtils.Create(new RepositoryUpdatedMessage { RepositoryId = repositoryId }, lastEditedBy: currentUser.UserId);

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

            var updated = await applicationDbContext.Repositories
                .Where(x => x.Id == repositoryId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (updated == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                };
            }

            return updated;
        }

        public async Task DeleteRepositoryAsync(int repositoryId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                };
            }

            bool isUpdateAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Writer, cancellationToken)
                .ConfigureAwait(false);

            if (!isUpdateAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                    UserId = currentUser.UserId,
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var repository = await applicationDbContext.Repositories.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == repositoryId, cancellationToken)
                .ConfigureAwait(false);

            if (repository == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                };
            }

            using (var transaction = await applicationDbContext.Database
                    .BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false))
            {
                var userRepositoryRoles = await applicationDbContext.UserRepositoryRoles.AsNoTracking()
                    .Where(t => t.Id == repository.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var teamRepositoryRoles = await applicationDbContext.TeamRepositoryRoles.AsNoTracking()
                    .Where(t => t.Id == repository.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                await applicationDbContext.UserRepositoryRoles
                        .Where(t => t.Id == repository.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);

                await applicationDbContext.TeamRepositoryRoles
                        .Where(t => t.Id == repository.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);

                await applicationDbContext.Repositories
                        .Where(t => t.Id == repository.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);

                var outboxEvent = new RepositoryDeletedMessage
                {
                    RepositoryId = repository.Id,
                    UserRepositoryRoles = userRepositoryRoles
                        .Select(x => new RemovedUserFromRepositoryMessage { UserId = x.UserId, RepositoryId = x.RepositoryId, Role = x.Role })
                        .ToList(),
                    TeamRepositoryRoles = teamRepositoryRoles
                        .Select(x => new RemovedTeamFromRepositoryMessage { TeamId = x.TeamId, RepositoryId = x.RepositoryId, Role = x.Role })
                        .ToList()
                };

                await applicationDbContext
                    .AddAsync(outboxEvent, cancellationToken)
                    .ConfigureAwait(false);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task<UserRepositoryRole> AddUserToRepositoryAsync(int userId, int repositoryId, RepositoryRoleEnum role, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                    UserId = currentUser.UserId,
                };
            }

            var userRepositoryRole = new UserRepositoryRole
            {
                RepositoryId = repositoryId,
                UserId = userId,
                Role = role,
                LastEditedBy = currentUser.UserId,
            };

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            await applicationDbContext
                .AddAsync(userRepositoryRole)
                .ConfigureAwait(false);

            var outboxEvent = OutboxEventUtils.Create(new AddedUserToRepositoryMessage
            {
                RepositoryId = userRepositoryRole.RepositoryId,
                UserId = userRepositoryRole.UserId,
                Role = userRepositoryRole.Role
            }, lastEditedBy: currentUser.UserId);

            await applicationDbContext
                .AddAsync(outboxEvent)
                .ConfigureAwait(false);

            await applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return userRepositoryRole;
        }

        public async Task RemoveUserFromRepositoryAsync(int userId, int repositoryId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                    UserId = currentUser.UserId,
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);


            var userRepositoryRole = await applicationDbContext.UserRepositoryRoles.AsNoTracking()
                .Where(x => x.UserId == userId && x.RepositoryId == repositoryId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (userRepositoryRole == null)
            {
                throw new UserNotAssignedToRepositoryException
                {
                    RepositoryId = repositoryId,
                    UserId = userId
                };
            }

            using (var transaction = await applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {

                await applicationDbContext.UserRepositoryRoles
                    .Where(x => x.Id == userRepositoryRole.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

                var outboxEvent = OutboxEventUtils.Create(new RemovedUserFromRepositoryMessage
                {
                    RepositoryId = userRepositoryRole.RepositoryId,
                    UserId = userRepositoryRole.UserId,
                    Role = userRepositoryRole.Role
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

        public async Task<List<UserRepositoryRole>> GetUserRepositoryRolesByRepositoryIdAsync(int repositoryId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);


            var userRepositoryRoles = await applicationDbContext.UserRepositoryRoles.AsNoTracking()
                .Where(x => x.RepositoryId == repositoryId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return userRepositoryRoles;
        }

        public async Task<List<TeamRepositoryRole>> GetTeamRepositoryRolesByRepositoryIdAsync(int repositoryId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var teamRepositoryRoles = await applicationDbContext.TeamRepositoryRoles.AsNoTracking()
                .Where(x => x.RepositoryId == repositoryId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return teamRepositoryRoles;
        }

        public async Task<TeamRepositoryRole> AddTeamToRepositoryAsync(int repositoryId, int teamId, RepositoryRoleEnum role, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                    UserId = currentUser.UserId,
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var teamIsAlreadyAssignedToRepository = await applicationDbContext.TeamRepositoryRoles.AsNoTracking()
                .Where(x => x.TeamId == teamId && x.RepositoryId == repositoryId)
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false);

            if (teamIsAlreadyAssignedToRepository)
            {
                throw new TeamAlreadyAssignedToRepositoryException
                {
                    RepositoryId = repositoryId,
                    TeamId = teamId
                };
            }

            var teamRepositoryRole = new TeamRepositoryRole
            {
                RepositoryId = repositoryId,
                TeamId = teamId,
                Role = role,
                LastEditedBy = currentUser.UserId,
            };

            await applicationDbContext
                .AddAsync(teamRepositoryRole, cancellationToken)
                .ConfigureAwait(false);

            var outboxEvent = OutboxEventUtils.Create<AddedTeamToRepositoryMessage>(new AddedTeamToRepositoryMessage
            {
                RepositoryId = teamRepositoryRole.RepositoryId,
                TeamId = teamRepositoryRole.TeamId,
                Role = teamRepositoryRole.Role,
            }, lastEditedBy: currentUser.UserId);

            await applicationDbContext
                .AddAsync(outboxEvent, cancellationToken)
                .ConfigureAwait(false);

            await applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return teamRepositoryRole;
        }

        public async Task RemoveTeamFromRepositoryAsync(int repositoryId, int teamId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                    UserId = currentUser.UserId,
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            using (var transaction = await applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                var teamRepositoryRole = await applicationDbContext.TeamRepositoryRoles.AsNoTracking()
                    .Where(x => x.TeamId == teamId && x.RepositoryId == repositoryId)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (teamRepositoryRole == null)
                {
                    throw new TeamNotAssignedToRepositoryException
                    {
                        RepositoryId = repositoryId,
                        TeamId = teamId
                    };
                }

                await applicationDbContext.TeamRepositoryRoles
                    .Where(x => x.Id == teamRepositoryRole.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

                var outboxEvent = OutboxEventUtils.Create<RemovedTeamFromRepositoryMessage>(new RemovedTeamFromRepositoryMessage
                {
                    TeamId = teamRepositoryRole.TeamId,
                    RepositoryId = teamRepositoryRole.RepositoryId,
                    Role = teamRepositoryRole.Role
                }, lastEditedBy: currentUser.UserId);

                await applicationDbContext.OutboxEvents
                    .AddAsync(outboxEvent)
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
