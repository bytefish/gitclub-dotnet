// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using GitClub.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GitClub.Services
{
    public class OrganizationService
    {
        private readonly ILogger<OrganizationService> _logger;

        private readonly ApplicationDbContext _applicationDbContext;
        private readonly AclService _aclService;

        public OrganizationService(ILogger<OrganizationService> logger, ApplicationDbContext applicationDbContext, AclService aclService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _aclService = aclService;
        }

        public async Task<Organization> CreateOrganizationAsync(Organization organization, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // Make sure the Current User is the last editor:
            organization.LastEditedBy = currentUserId;

            // Add the new Task, the HiLo Pattern automatically assigns a new Id using the HiLo Pattern
            await _applicationDbContext
                .AddAsync(organization, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Write Tuples to Zanzibar
            var relationsToWrite = new[]
            {
                RelationTuple.Create<Organization, Organization>(organization, organization, organization.BaseRepositoryRole.ToString(), Relations.Member),
                RelationTuple.Create<Organization, User>(organization.Id, currentUserId, OrganizationRoleEnum.Owner.ToString()),
            };

            await _aclService
                .AddRelationshipsAsync(relationsToWrite, cancellationToken)
                .ConfigureAwait(false);

            return organization;
        }

        public async Task<Organization> GetOrganizationByIdAsync(int organizationId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUserId, organizationId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                };
            }

            var organization = await _applicationDbContext.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                };
            }

            return organization;
        }

        public async Task<List<Organization>> GetOrganizationsByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var organizations = await _aclService
                .ListUserObjectsAsync<Organization>(userId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            return organizations;
        }

        public async Task<Organization> UpdateOrganizationAsync(int organizationId, Organization values, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUserId, organizationId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                };
            }

            bool isUpdateAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUserId, organizationId, Actions.CanWrite, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                    UserId = currentUserId,
                };
            }

            var organization = await _applicationDbContext.Organizations
                .Where(x => x.Id == organizationId)
                .FirstAsync(cancellationToken);

            if (organization == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                };
            }

            int rowsAffected = await _applicationDbContext.Organizations
                .Where(t => t.Id == organization.Id && t.RowVersion == organization.RowVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Name, organization.Name)
                    .SetProperty(x => x.BaseRepositoryRole, organization.BaseRepositoryRole)
                    .SetProperty(x => x.BillingAddress, organization.BillingAddress)
                    .SetProperty(x => x.LastEditedBy, currentUserId), cancellationToken)
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new EntityConcurrencyException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organization.Id,
                };
            }

            var relationsToWrite = new RelationTuple
            {
                Object = $"Organization:{organizationId}",
                Relation = baseRepositoryRole,
                Subject = $"Organization:{organizationId}#member"
            };

            var relationsToDelete = new RelationTuple
            {
                Object = $"Organization:{organizationId}",
                Relation = organization.BaseRepositoryRole,
                Subject = $"Organization:{organizationId}#member"
            };

            await _aclService
                .WriteAsync([relationsToWrite], [relationsToDelete], cancellationToken)
                .ConfigureAwait(false);



            return organization;
        }

        public async Task DeleteOrganizationAsync(int organizationId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUserId, organizationId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                };
            }

            var organization = await _applicationDbContext.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                };
            }

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUserId, organizationId, Actions.CanWrite, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                    UserId = currentUserId,
                };
            }

            await _applicationDbContext.Organizations
                    .Where(t => t.Id == organization.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

            // TODO Delete all Tuples for the Organization

        }

        public async Task<UserOrganizationRole> AddUserToOrganizationAsync(int organizationId, int userId, OrganizationRoleEnum role, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUserId, organizationId, Actions.CanWrite, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                    UserId = currentUserId,
                };
            }

            var organizationRole = new UserOrganizationRole
            {
                OrganizationId = organizationId,
                UserId = userId,
                Role = role,
                LastEditedBy = currentUserId,
            };

            await _applicationDbContext
                .AddAsync(organizationRole)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Write Tuples to Zanzibar
            var relationsToWrite = new[]
            {
                RelationTuple.Create<Organization, User>(organizationId, currentUserId, role.ToString()),
            };

            await _aclService
                .AddRelationshipsAsync(relationsToWrite, cancellationToken)
                .ConfigureAwait(false);

            return organizationRole;
        }

        public async Task RemoveUserFromOrganizationAsync(int organizationId, int userId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUserId, organizationId, Actions.CanWrite, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                    UserId = currentUserId,
                };
            }

            var organizationRole = await _applicationDbContext.UserOrganizationRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == organizationId && x.UserId == userId)
                .ConfigureAwait(false);

            if(organizationRole == null)
            {
                throw new UserNotAssignedToOrganizationException
                {
                    OrganizationId = organizationId,
                    UserId = userId
                };
            }

            int rowsAffected = await _applicationDbContext.UserOrganizationRoles
                .Where(x => x.Id == organizationRole.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            // Delete Tuple from Zanzibar
            var relationsToWrite = new[]
            {
                RelationTuple.Create<Organization, User>(organizationId, currentUserId, organizationRole.Role.ToString()),
            };

            await _aclService
                .DeleteRelationshipAsync<Organization, User>(organizationId, Relations.Member, userId, null, cancellationToken)
                .ConfigureAwait(false);
        }

    }
}
