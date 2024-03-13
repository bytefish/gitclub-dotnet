// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;

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

            // Make sure the Current User is the last editor:
            issue.LastEditedBy = currentUserId;

            // Add the new Task, the HiLo Pattern automatically assigns a new Id using the HiLo Pattern
            await _applicationDbContext
                .AddAsync(issue, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            // Add to ACL Store
            await _aclService
                .AddRelationshipAsync<Issue, User>(issue.Id, Relations.Owner, currentUserId, null)
                .ConfigureAwait(false);

            return issue;
        }

        public async Task<Issue> GetIssueByIdAsync(int issueId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Issue>(currentUserId, issueId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            var issue = await _applicationDbContext.Issues
                .AsNoTracking()
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

            return issue;
        }

        public async Task<List<Issue>> GetIssuesByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var issues = await _aclService
                .ListUserObjectsAsync<Issue>(userId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            return issues;
        }

        public async Task<Issue> UpdateIssueAsync(int issueId, Issue issue, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Issue>(currentUserId, issueId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issue.Id,
                };
            }

            bool isUpdateAuthorized = await _aclService
                .CheckUserObjectAsync(currentUserId, issue, Actions.CanWrite, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issue.Id,
                    UserId = currentUserId,
                };
            }

            int rowsAffected = await _applicationDbContext.Issues
                .Where(t => t.Id == issueId && t.RowVersion == issue.RowVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Title, issue.Title)
                    .SetProperty(x => x.Content, issue.Content)
                    .SetProperty(x => x.Closed, issue.Closed)
                    .SetProperty(x => x.RepositoryId, issue.RepositoryId)
                    .SetProperty(x => x.LastEditedBy, currentUserId), cancellationToken)
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new EntityConcurrencyException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issue.Id,
                };
            }

            return issue;
        }

        public async Task DeleteIssueAsync(int issueId, int currentUserId, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Issue>(currentUserId, issueId, Actions.CanRead, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            var issue = await _applicationDbContext.Issues
                .AsNoTracking()
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

            bool isAuthorized = await _aclService
                .CheckUserObjectAsync<Issue>(currentUserId, issueId, Actions.CanWrite, cancellationToken)
                .ConfigureAwait(false);

            if (!isAuthorized)
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

            // TODO Delete all Tuples for the Issue
        }

    }
}
