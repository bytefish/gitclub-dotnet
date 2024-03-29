﻿// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.OpenFga;
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

        public async Task<Organization> CreateOrganizationAsync(Organization organization, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = currentUser.IsInRole(Roles.Administrator);

            if(!isAuthorized)
            {
                throw new AuthorizationFailedException("Insufficient Permissions to create an Organization");
            }

            organization.LastEditedBy = currentUser.UserId;

            await _applicationDbContext
                .AddAsync(organization, cancellationToken)
                .ConfigureAwait(false);

            // The User creating the Organization is automatically the Owner
            var userOrganizationRole = new UserOrganizationRole
            {
                OrganizationId = organization.Id,
                Role = OrganizationRoleEnum.Owner,
                UserId = currentUser.UserId,
                LastEditedBy = currentUser.UserId
            };

            await _applicationDbContext
                .AddAsync(userOrganizationRole, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Write Tuples to Zanzibar
            var tuplesToWrite = new[]
            {
                RelationTuples.Create<Organization, Organization>(organization, organization, organization.BaseRepositoryRole, Relations.Member),
                RelationTuples.Create<Organization, User>(userOrganizationRole.OrganizationId, userOrganizationRole.UserId, userOrganizationRole.Role),
            };

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);

            return organization;
        }

        public async Task<Organization> GetOrganizationByIdAsync(int organizationId, CurrentUser currentUser, CancellationToken cancellationToken)
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

            var organization = await _applicationDbContext.Organizations.AsNoTracking()
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

        public async Task<List<Organization>> GetOrganizationsAsync(CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var organizations = await _aclService
                .ListUserObjectsAsync<Organization>(currentUser.UserId, OrganizationRoleEnum.Member.AsRelation(), cancellationToken)
                .ConfigureAwait(false);

            return organizations;
        }

        public async Task<Organization> UpdateOrganizationAsync(int organizationId, Organization values, CurrentUser currentUser, CancellationToken cancellationToken)
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

            bool isUpdateAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUser.UserId, organizationId, OrganizationRoleEnum.Administrator, cancellationToken)
                .ConfigureAwait(false);

            if (!isUpdateAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                    UserId = currentUser.UserId,
                };
            }

            var original = await _applicationDbContext.Organizations.AsNoTracking()
                .Where(x => x.Id == organizationId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (original == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                };
            }

            int rowsAffected = await _applicationDbContext.Organizations.AsNoTracking()
                .Where(t => t.Id == organizationId && t.RowVersion == values.RowVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Name, values.Name)
                    .SetProperty(x => x.BaseRepositoryRole, values.BaseRepositoryRole)
                    .SetProperty(x => x.BillingAddress, values.BillingAddress)
                    .SetProperty(x => x.LastEditedBy, currentUser.UserId), cancellationToken)
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new EntityConcurrencyException()
                {
                    EntityName = nameof(Organization),
                    EntityId = original.Id,
                };
            }

            var tuplesToWrite = new[] 
            {
                RelationTuples.Create<Organization, Organization>(organizationId, organizationId, values.BaseRepositoryRole, Relations.Member)
            };

            var tuplesToDelete = new []
            {
                RelationTuples.Create<Organization, Organization>(organizationId, organizationId, original.BaseRepositoryRole, Relations.Member)
            };

            await _aclService
                .WriteAsync(tuplesToWrite, tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);

            var updated = await _applicationDbContext.Organizations.AsNoTracking()
                .Where(x => x.Id == organizationId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (updated == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                };
            }

            return updated;
        }

        public async Task DeleteOrganizationAsync(int organizationId, CurrentUser currentUser, CancellationToken cancellationToken)
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

            var organization = await _applicationDbContext.Organizations.AsNoTracking()
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

            bool isDeleteAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUser.UserId, organizationId, OrganizationRoleEnum.Owner, cancellationToken)
                .ConfigureAwait(false);

            if (!isDeleteAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                    UserId = currentUser.UserId,
                };
            }

            using (var transaction = await _applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {                
                // Get dependent data ...
                var userOrganizationRoles = await _applicationDbContext.UserOrganizationRoles.AsNoTracking()
                    .Where(x => x.OrganizationId == organizationId)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Run deletes ...
                await _applicationDbContext.UserOrganizationRoles
                    .Where(x => x.OrganizationId == organization.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

                await _applicationDbContext.Organizations
                        .Where(t => t.Id == organization.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Delete Relations from Zanzibar
                List<RelationTuple> tuplesToDelete =
                [
                    RelationTuples.Create<Organization, Organization>(organizationId, organizationId, organization.BaseRepositoryRole, Relations.Member)
                ];

                foreach(var userOrganizationRole in userOrganizationRoles)
                {
                    var tuple = RelationTuples.Create<Organization, User>(userOrganizationRole.OrganizationId, userOrganizationRole.UserId, userOrganizationRole.Role);

                    tuplesToDelete.Add(tuple);
                }

                await _aclService.DeleteRelationshipsAsync(tuplesToDelete, cancellationToken);
            }
        }

        public async Task<List<UserOrganizationRole>> GetUserOrganizationRolesByOrganizationIdAsync(int organizationId, CurrentUser currentUser, CancellationToken cancellationToken)
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


            var userOrganizationRoles = await _applicationDbContext.UserOrganizationRoles.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return userOrganizationRoles;
        }

        public async Task<List<UserOrganizationRole>> GetUserOrganizationRolesByOrganizationIdAndRoleAsync(int organizationId, OrganizationRoleEnum role, CurrentUser currentUser, CancellationToken cancellationToken)
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

            var userOrganizationRoles = await _applicationDbContext.UserOrganizationRoles.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Role == role)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return userOrganizationRoles;
        }

        public async Task<UserOrganizationRole> AddUserToOrganizationAsync(int userId, int organizationId, OrganizationRoleEnum role, CurrentUser currentUser, CancellationToken cancellationToken)
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

            bool isUpdateAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUser.UserId, organizationId, OrganizationRoleEnum.Owner, cancellationToken)
                .ConfigureAwait(false);

            if (!isUpdateAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                    UserId = currentUser.UserId,
                };
            }

            var userIsAlreadyAssignedToOrganization = await _applicationDbContext.UserOrganizationRoles.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.UserId == userId)
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false);

            if (userIsAlreadyAssignedToOrganization)
            {
                throw new UserAlreadyAssignedToOrganizationException
                {
                    OrganizationId = organizationId,
                    UserId = userId
                };
            }

            var organizationRole = new UserOrganizationRole
            {
                OrganizationId = organizationId,
                UserId = userId,
                Role = role,
                LastEditedBy = currentUser.UserId,
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
                RelationTuples.Create<Organization, User>(organizationRole.OrganizationId, organizationRole.UserId, organizationRole.Role),
            };

            await _aclService
                .AddRelationshipsAsync(relationsToWrite, cancellationToken)
                .ConfigureAwait(false);

            return organizationRole;
        }

        public async Task RemoveUserFromOrganizationAsync(int userId, int organizationId, OrganizationRoleEnum role, CurrentUser currentUser, CancellationToken cancellationToken)
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

            bool isUpdateAuthorized = await _aclService
                .CheckUserObjectAsync<Organization>(currentUser.UserId, organizationId, OrganizationRoleEnum.Owner, cancellationToken)
                .ConfigureAwait(false);

            if (!isUpdateAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Organization),
                    EntityId = organizationId,
                    UserId = currentUser.UserId,
                };
            }

            var organizationRole = await _applicationDbContext.UserOrganizationRoles.AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == userId && x.Role == role)
                .ConfigureAwait(false);

            if(organizationRole == null)
            {
                throw new UserNotAssignedToOrganizationInRoleException
                {
                    OrganizationId = organizationId,
                    UserId = userId,
                    Role = role
                };
            }

            int rowsAffected = await _applicationDbContext.UserOrganizationRoles
                .Where(x => x.Id == organizationRole.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            // Delete Tuple from Zanzibar
            var relationsToDelete = new[]
            {
                RelationTuples.Create<Organization, User>(organizationId, userId, organizationRole.Role),
            };

            await _aclService
                .DeleteRelationshipsAsync(relationsToDelete, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
