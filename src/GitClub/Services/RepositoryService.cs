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

        public async Task<Repository> CreateRepositoryAsync(Repository repository, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUserId, repository.OrganizationId, OrganizationRoleEnum.Member, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException
                {
                    EntityName = nameof(Organization),
                    EntityId = repository.OrganizationId,
                    UserId = currentUserId
                };
            }

            // Make sure the Current User is the editor:
            repository.LastEditedBy = currentUserId;
            
            await _applicationDbContext
                .AddAsync(repository, cancellationToken)
                .ConfigureAwait(false);

            // The User, that created the Repository is initially
            // also the Administrator of the Repository.
            var userRepositoryRole = new UserRepositoryRole
            {
                RepositoryId = repository.Id,
                UserId = currentUserId,
                Role = RepositoryRoleEnum.Administrator,
                LastEditedBy = currentUserId
            };

            await _applicationDbContext
                .AddAsync(userRepositoryRole);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Write tuples to Zanzibar
            var tuplesToWrite = new[]
            {                
                // The Organization is the Owner of the Repository 
                RelationTuples.Create<Repository, Organization>(repository.Id, repository.OrganizationId, RepositoryRoleEnum.Owner), // The Organization becomes the Owner of the Repository
                // The current User is the Administrator of the Repository
                RelationTuples.Create<Repository, User>(repository.Id, currentUserId, RepositoryRoleEnum.Administrator),
            };

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);

            return repository;
        }

        public async Task<Repository> GetRepositoryByIdAsync(int repositoryId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Reader, cancellationToken)
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

        public async Task<List<Repository>> GetRepositoriesByOrganizationIdAsync(int organizationId, int userId, CancellationToken cancellationToken)
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

            var repositories = await _applicationDbContext.Repositories.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId)
                .ToListAsync()
                .ConfigureAwait(false);

            return repositories;
        }

        public async Task<List<Repository>> GetRepositoriesByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var repositories = await _aclService
                .ListUserObjectsAsync<Repository>(userId, RepositoryRoleEnum.Reader.AsRelation(), cancellationToken)
                .ConfigureAwait(false);

            return repositories;
        }

        public async Task<Repository> UpdateRepositoryAsync(int repositoryId, Repository values, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Reader, cancellationToken)
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
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Writer, cancellationToken)
                .ConfigureAwait(false);

            if (!isUpdateAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = values.Id,
                    UserId = currentUserId,
                };
            }

            int rowsAffected = await _applicationDbContext.Repositories
                .Where(t => t.Id == repositoryId && t.RowVersion == values.RowVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Name, values.Name)
                    .SetProperty(x => x.OrganizationId, values.OrganizationId)
                    .SetProperty(x => x.LastEditedBy, currentUserId), cancellationToken)
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new EntityConcurrencyException()
                {
                    EntityName = nameof(Repository),
                    EntityId = values.Id,
                };
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

        public async Task DeleteRepositoryAsync(int repositoryId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Maintainer, cancellationToken)
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
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Writer, cancellationToken)
                .ConfigureAwait(false);

            if (!isUpdateAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                    UserId = currentUserId,
                };
            }

            using (var transaction = await _applicationDbContext.Database
                    .BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false))
            {

                var userRepositoryRoles = await _applicationDbContext.UserRepositoryRoles.AsNoTracking()
                    .Where(t => t.Id == repository.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var teamRepositoryRoles = await _applicationDbContext.TeamRepositoryRoles.AsNoTracking()
                    .Where(t => t.Id == repository.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

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

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Delete tuples in Zanzibar
                List<RelationTuple> tuplesToDelete = [];

                foreach (var userRepositoryRole in userRepositoryRoles)
                {
                    var tuple = RelationTuples.Create<Repository, User>(userRepositoryRole.RepositoryId, userRepositoryRole.UserId, userRepositoryRole.Role);

                    tuplesToDelete.Add(tuple);
                }

                foreach (var teamRepositoryRole in teamRepositoryRoles)
                {
                    var tuple = RelationTuples.Create<Repository, Team>(teamRepositoryRole.RepositoryId, teamRepositoryRole.TeamId, teamRepositoryRole.Role);

                    tuplesToDelete.Add(tuple);
                }

                await _aclService
                    .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task<UserRepositoryRole> AddUserToRepositoryAsync(int repositoryId, int userId, RepositoryRoleEnum role, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                    UserId = currentUserId,
                };
            }

            var repositoryRole = new UserRepositoryRole
            {
                RepositoryId = repositoryId,
                UserId = userId,
                Role = role,
                LastEditedBy = currentUserId,
            };

            await _applicationDbContext
                .AddAsync(repositoryRole)
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

        public async Task RemoveUserFromRepositoryAsync(int repositoryId, int userId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                    UserId = currentUserId,
                };
            }

            var userRepositoryRole = await _applicationDbContext.UserRepositoryRoles.AsNoTracking()
                .Where(x => x.UserId == userId && x.RepositoryId == repositoryId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if(userRepositoryRole == null)
            {
                throw new UserNotAssignedToRepositoryException
                {
                    RepositoryId = repositoryId,
                    UserId = userId
                };
            }

            await _applicationDbContext.UserRepositoryRoles
                .Where(x => x.Id == userRepositoryRole.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            // Delete Tuples from Zanzibar
            var tuplesToDelete = new[]
            {
                RelationTuples.Create<Repository, User>(userRepositoryRole.RepositoryId, userRepositoryRole.UserId, userRepositoryRole.Role)
            };

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<UserRepositoryRole>> GetUserRepositoryRolesByRepositoryIdAsync(int repositoryId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Reader, cancellationToken)
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

        public async Task<List<TeamRepositoryRole>> GetTeamRepositoryRolesByRepositoryIdAsync(int repositoryId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Reader, cancellationToken)
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

        public async Task<TeamRepositoryRole> AddTeamToRepositoryAsync(int repositoryId, int teamId, RepositoryRoleEnum role, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                    UserId = currentUserId,
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
                LastEditedBy = currentUserId,
            };

            await _applicationDbContext
                .AddAsync(teamRepositoryRole)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Write Tuples to Zanzibar
            var tuplesToWrite = new[]
            {
                RelationTuples.Create<Repository, Team>(repositoryId, teamId, role, TeamRoleEnum.Member.AsRelation())
            };

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);

            return teamRepositoryRole;
        }

        public async Task RemoveTeamFromRepositoryAsync(int repositoryId, int teamId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, RepositoryRoleEnum.Maintainer, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                    UserId = currentUserId,
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

            await _applicationDbContext.UserRepositoryRoles
                .Where(x => x.Id == teamRepositoryRole.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            // Delete Tuples from Zanzibar
            var tuplesToDelete = new[]
            {
                RelationTuples.Create<Repository, Team>(teamRepositoryRole.RepositoryId, teamRepositoryRole.TeamId, teamRepositoryRole.Role)
            };

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
