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

        private readonly ApplicationDbContext _applicationDbContext;
        private readonly AclService _aclService;

        public RepositoryService(ILogger<RepositoryService> logger, ApplicationDbContext applicationDbContext, AclService aclService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
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

            // Make sure the Current User is the editor:
            repository.LastEditedBy = currentUser.UserId;

            await _applicationDbContext
                .AddAsync(repository, cancellationToken)
                .ConfigureAwait(false);

            // The User, that created the Repository is initially
            // also the Administrator of the Repository.
            var userRepositoryRole = new UserRepositoryRole
            {
                RepositoryId = repository.Id,
                UserId = currentUser.UserId,
                Role = RepositoryRoleEnum.Administrator,
                LastEditedBy = currentUser.UserId
            };

            await _applicationDbContext
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

            await _applicationDbContext
                .AddAsync(outboxEvent, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            //// Write tuples to Zanzibar
            //var tuplesToWrite = new[]
            //{                
            //    // The Organization is the Owner of the Repository 
            //    RelationTuples.Create<Repository, Organization>(repository.Id, repository.OrganizationId, RepositoryRoleEnum.Owner), // The Organization becomes the Owner of the Repository
            //    // The current User is the Administrator of the Repository
            //    RelationTuples.Create<Repository, User>(repository.Id, currentUser.UserId, RepositoryRoleEnum.Administrator),
            //};

            //await _aclService
            //    .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
            //    .ConfigureAwait(false);

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

            var repository = await _applicationDbContext.Repositories.AsNoTracking()
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

            var repositories = await _applicationDbContext.Repositories.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId)
                .ToListAsync()
                .ConfigureAwait(false);

            return repositories;
        }

        public async Task<List<Repository>> GetRepositoriesAsync(CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var repositories = await _aclService
                .ListUserObjectsAsync<Repository>(currentUser.UserId, RepositoryRoleEnum.Reader.AsRelation(), cancellationToken)
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

            using (var transaction = await _applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                int rowsAffected = await _applicationDbContext.Repositories
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

                await _applicationDbContext
                    .AddAsync(outboxEvent, cancellationToken)
                    .ConfigureAwait(false);

                await _applicationDbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            var updated = await _applicationDbContext.Repositories
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

            var repository = await _applicationDbContext.Repositories.AsNoTracking()
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

            var userRepositoryRoles = await _applicationDbContext.UserRepositoryRoles.AsNoTracking()
                .Where(t => t.Id == repository.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var teamRepositoryRoles = await _applicationDbContext.TeamRepositoryRoles.AsNoTracking()
                .Where(t => t.Id == repository.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            using (var transaction = await _applicationDbContext.Database
                    .BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false))
            {
                await _applicationDbContext.UserRepositoryRoles
                        .Where(t => t.Id == repository.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);

                await _applicationDbContext.TeamRepositoryRoles
                        .Where(t => t.Id == repository.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);

                await _applicationDbContext.Repositories
                        .Where(t => t.Id == repository.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);

                var outboxEvent = new RepositoryDeletedMessage
                {
                    RepositoryId = repository.Id,
                    UserRepositoryRoles = userRepositoryRoles
                        .Select(x => new RemovedUserFromRepositoryMessage { UserId = x.UserId, RepositoryId = x.RepositoryId })
                        .ToList(),
                    TeamRepositoryRoles = teamRepositoryRoles
                        .Select(x => new RemovedTeamFromRepositoryMessage { TeamId = x.TeamId, RepositoryId = x.RepositoryId })
                        .ToList()
                };

                await _applicationDbContext
                    .AddAsync(outboxEvent, cancellationToken)
                    .ConfigureAwait(false);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Delete tuples in Zanzibar
                //List<RelationTuple> tuplesToDelete = [];

                //foreach (var userRepositoryRole in userRepositoryRoles)
                //{
                //    var tuple = RelationTuples.Create<Repository, User>(userRepositoryRole.RepositoryId, userRepositoryRole.UserId, userRepositoryRole.Role);

                //    tuplesToDelete.Add(tuple);
                //}

                //foreach (var teamRepositoryRole in teamRepositoryRoles)
                //{
                //    var tuple = RelationTuples.Create<Repository, Team>(teamRepositoryRole.RepositoryId, teamRepositoryRole.TeamId, teamRepositoryRole.Role);

                //    tuplesToDelete.Add(tuple);
                //}

                //await _aclService
                //    .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                //    .ConfigureAwait(false);
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

            var repositoryRole = new UserRepositoryRole
            {
                RepositoryId = repositoryId,
                UserId = userId,
                Role = role,
                LastEditedBy = currentUser.UserId,
            };

            await _applicationDbContext
                .AddAsync(repositoryRole)
                .ConfigureAwait(false);

            var outboxEvent = new AddedUserToRepositoryMessage
            {
                RepositoryId = repositoryRole.RepositoryId,
                UserId = repositoryRole.UserId,
                Role = repositoryRole.Role
            };

            await _applicationDbContext
                .AddAsync(outboxEvent)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Write Tuples to Zanzibar
            var tuplesToWrite = new[]
            {
                RelationTuples.Create<Repository, User>(repositoryRole.RepositoryId, repositoryRole.UserId, repositoryRole.Role)
            };

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);

            return repositoryRole;
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

            var userRepositoryRole = await _applicationDbContext.UserRepositoryRoles.AsNoTracking()
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

            using (var transaction = await _applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {

                await _applicationDbContext.UserRepositoryRoles
                    .Where(x => x.Id == userRepositoryRole.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

                var outboxEvent = OutboxEventUtils.Create(new RemovedUserFromRepositoryMessage
                {
                    RepositoryId = userRepositoryRole.RepositoryId,
                    UserId = userRepositoryRole.UserId,
                }, lastEditedBy: currentUser.UserId);

                await _applicationDbContext
                    .AddAsync(outboxEvent, cancellationToken)
                    .ConfigureAwait(false);

                await _applicationDbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            //// Delete Tuples from Zanzibar
            //var tuplesToDelete = new[]
            //{
            //    RelationTuples.Create<Repository, User>(userRepositoryRole.RepositoryId, userRepositoryRole.UserId, userRepositoryRole.Role)
            //};

            //await _aclService
            //    .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
            //    .ConfigureAwait(false);
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

            var userRepositoryRoles = await _applicationDbContext.UserRepositoryRoles.AsNoTracking()
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

            var teamRepositoryRoles = await _applicationDbContext.TeamRepositoryRoles.AsNoTracking()
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

            var teamIsAlreadyAssignedToRepository = await _applicationDbContext.TeamRepositoryRoles.AsNoTracking()
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

            await _applicationDbContext
                .AddAsync(teamRepositoryRole, cancellationToken)
                .ConfigureAwait(false);

            var outboxEvent = OutboxEventUtils.Create<AddedTeamToRepositoryMessage>(new AddedTeamToRepositoryMessage
            {
                RepositoryId = teamRepositoryRole.RepositoryId,
                TeamId = teamRepositoryRole.TeamId,
                Role = teamRepositoryRole.Role,
            }, lastEditedBy: currentUser.UserId);

            await _applicationDbContext
                .AddAsync(outboxEvent, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Write Tuples to Zanzibar
            //var tuplesToWrite = new[]
            //{
            //    RelationTuples.Create<Repository, Team>(repositoryId, teamId, role, TeamRoleEnum.Member.AsRelation())
            //};

            //await _aclService
            //    .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
            //    .ConfigureAwait(false);

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

            var teamRepositoryRole = await _applicationDbContext.TeamRepositoryRoles.AsNoTracking()
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
            using (var transaction = await _applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                await _applicationDbContext.TeamRepositoryRoles
                    .Where(x => x.Id == teamRepositoryRole.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

                var outboxEvent = OutboxEventUtils.Create<RemovedTeamFromRepositoryMessage>(new RemovedTeamFromRepositoryMessage
                {
                    TeamId = teamRepositoryRole.TeamId,
                    RepositoryId = teamRepositoryRole.RepositoryId
                }, lastEditedBy: currentUser.UserId);

                await _applicationDbContext.OutboxEvents
                    .AddAsync(outboxEvent)
                    .ConfigureAwait(false);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // Delete Tuples from Zanzibar
            var tuplesToDelete = new[]
            {
                RelationTuples.Create<Repository, Team>(teamRepositoryRole.RepositoryId, teamRepositoryRole.TeamId, teamRepositoryRole.Role, TeamRoleEnum.Member.AsRelation())
            };

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
