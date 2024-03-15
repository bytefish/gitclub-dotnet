// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
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

            // Make sure the Current User is the last editor:
            repository.LastEditedBy = currentUserId;

            // Add the new Task, the HiLo Pattern automatically assigns a new Id using the HiLo Pattern
            await _applicationDbContext
                .AddAsync(repository, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Add to ACL Store
            await _aclService
                .AddRelationshipAsync<Repository, User>(repository.Id, Relations.Owner, currentUserId, null)
                .ConfigureAwait(false);

            return repository;
        }

        public async Task<Repository> GetRepositoryByIdAsync(int repositoryId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                };
            }

            var repository = await _applicationDbContext.Repositories
                .AsNoTracking()
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

            var repositories = await _applicationDbContext.Repositories
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId)
                .ToListAsync()
                .ConfigureAwait(false);

            return repositories;
        }

        public async Task<List<Repository>> GetRepositorysByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var repositories = await _aclService
                .ListUserObjectsAsync<Repository>(userId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            return repositories;
        }

        public async Task<Repository> UpdateRepositoryAsync(int repositoryId, Repository repository, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repository.Id,
                };
            }

            bool isUpdateAuthorized = await _aclService
                .CheckUserObjectAsync(currentUserId, repository, Actions.CanWrite, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repository.Id,
                    UserId = currentUserId,
                };
            }

            int rowsAffected = await _applicationDbContext.Repositories
                .Where(t => t.Id == repositoryId && t.RowVersion == repository.RowVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Name, repository.Name)
                    .SetProperty(x => x.OrganizationId, repository.OrganizationId)
                    .SetProperty(x => x.LastEditedBy, currentUserId), cancellationToken)
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new EntityConcurrencyException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repository.Id,
                };
            }

            return repository;
        }

        public async Task DeleteRepositoryAsync(int repositoryId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Repository),
                    EntityId = repositoryId,
                };
            }

            var repository = await _applicationDbContext.Repositories
                .AsNoTracking()
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

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, Actions.CanWrite, cancellationToken)
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

            await _applicationDbContext.Repositories
                    .Where(t => t.Id == repository.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

            // TODO Delete all Tuples for the Repository
        }

        public async Task<UserRepositoryRole> AddUserToRepositoryAsync(int repositoryId, int userId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, Actions.CanWrite, cancellationToken)
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
                Role = Relations.Member,
                LastEditedBy = currentUserId,
            };

            await _applicationDbContext
                .AddAsync(repositoryRole)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            await _aclService
                .AddRelationshipAsync<Repository, User>(repositoryId, Relations.Member, userId, null)
                .ConfigureAwait(false);

            return repositoryRole;
        }

        public async Task RemoveUserFromRepositoryAsync(int repositoryId, int userId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, repositoryId, Actions.CanWrite, cancellationToken)
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

            await _applicationDbContext
                .RepositoryRoles
                .Where(x => x.Id == repositoryId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            await _aclService
                .DeleteRelationshipAsync<Repository, User>(repositoryId, Relations.Member, userId, null, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
