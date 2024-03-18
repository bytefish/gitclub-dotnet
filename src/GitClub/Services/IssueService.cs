// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using GitClub.Infrastructure.OpenFga;
using GitClub.Models;

namespace GitClub.Services
{
    public class IssueService
    {
        private readonly ILogger<IssueService> _logger;

        private readonly ApplicationDbContext _applicationDbContext;
        private readonly AclService _aclService;

        public IssueService(ILogger<IssueService> logger, ApplicationDbContext applicationDbContext, AclService aclService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _aclService = aclService;
        }

        public async Task<Issue> CreateIssueAsync(Issue issue, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // Make sure the current user is the last editor:
            issue.LastEditedBy = currentUserId;

            // Make sure the current user is the creator:
            issue.CreatedBy = currentUserId;

            // Add the new Task, the HiLo Pattern automatically assigns a new Id using the HiLo Pattern
            await _applicationDbContext
                .AddAsync(issue, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Write tuples to Zanzibar
            var tuplesToWrite = new[]
            {
                RelationTuple.Create<Issue, User>(issue.Id, currentUserId, Relations.Creator),
                RelationTuple.Create<Issue, Repository>(issue.Id, issue.RepositoryId, Relations.Owner)
            };

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);

            return issue;
        }

        public async Task<Issue> GetIssueByIdAsync(int issueId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var issue = await _applicationDbContext.Issues.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == issueId, cancellationToken)
                .ConfigureAwait(false);

            if (issue == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, issue.RepositoryId, RepositoryRoleEnum.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            return issue;
        }

        public async Task<List<Issue>> GetIssuesByRepositoryIdAsync(int repositoryId, int userId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(userId, repositoryId, RepositoryRoleEnum.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = repositoryId,
                };
            }

            var issues = await _applicationDbContext.Issues
                .AsNoTracking()
                .Where(x => x.RepositoryId == repositoryId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return issues;
        }

        public async Task<List<Issue>> GetIssuesByOrganizationIdAsync(int organizationId, int userId, CancellationToken cancellationToken)
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

            var query = from organization in _applicationDbContext.Organizations
                            join repository in _applicationDbContext.Repositories on organization.Id equals repository.OrganizationId
                            join issue in _applicationDbContext.Issues on repository.Id equals issue.RepositoryId
                        where organization.Id.Equals(organizationId)
                        select issue;

            var issues = await query.AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return issues;
        }

        public async Task<List<Issue>> GetIssuesByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var repositories = await _aclService
                .ListUserObjectsAsync<Repository>(userId, RepositoryRoleEnum.Reader.AsRelation(), cancellationToken)
                .ConfigureAwait(false);

            var repositoryIds = repositories
                .Select(x => x.Id)
                .ToList();

            var issues = await _applicationDbContext.Issues.AsNoTracking()
                .Where(x => repositoryIds.Contains(x.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return issues;
        }

        public async Task<Issue> UpdateIssueAsync(int issueId, Issue values, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var original = await _applicationDbContext.Issues.AsNoTracking()
                .Where(x => x.Id == issueId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if(original == null)
            {
                throw new EntityNotFoundException
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUserId, original.RepositoryId, RepositoryRoleEnum.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            int rowsAffected = await _applicationDbContext.Issues
                .Where(t => t.Id == issueId && t.RowVersion == values.RowVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Title, values.Title)
                    .SetProperty(x => x.Content, values.Content)
                    .SetProperty(x => x.Closed, values.Closed)
                    .SetProperty(x => x.LastEditedBy, currentUserId), cancellationToken)
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new EntityConcurrencyException()
                {
                    EntityName = nameof(Issue),
                    EntityId = values.Id,
                };
            }

            var updated = await _applicationDbContext.Issues.AsNoTracking()
                .Where(x => x.Id == issueId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (updated == null)
            {
                throw new EntityNotFoundException
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            return updated;
        }

        public async Task DeleteIssueAsync(int issueId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Issue>(currentUserId, issueId, Relations.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            var issue = await _applicationDbContext.Issues.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == issueId, cancellationToken)
                .ConfigureAwait(false);

            if (issue == null)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            bool isDeleteAuthorized = await _aclService
                .CheckUserObjectAsync<Issue>(currentUserId, issueId, Relations.Writer, cancellationToken)
                .ConfigureAwait(false);

            if (!isDeleteAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                    UserId = currentUserId,
                };
            }

            await _applicationDbContext.Issues
                    .Where(t => t.Id == issue.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

            // Delete tuples from Zanzibar
            var tuplesToDelete = new[]
            {
                RelationTuple.Create<Issue, User>(issue.Id, currentUserId, Relations.Creator),
                RelationTuple.Create<Issue, Repository>(issue.Id, issue.RepositoryId, Relations.Owner)
            };

            await _aclService
                .AddRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
