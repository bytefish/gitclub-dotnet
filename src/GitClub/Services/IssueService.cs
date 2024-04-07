// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using GitClub.Infrastructure.OpenFga;
using GitClub.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Outbox;
using GitClub.Infrastructure.Outbox.Messages;

namespace GitClub.Services
{
    public class IssueService
    {
        private readonly ILogger<IssueService> _logger;

        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly AclService _aclService;

        public IssueService(ILogger<IssueService> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, AclService aclService)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _aclService = aclService;
        }

        public async Task<Issue> CreateIssueAsync(Issue issue, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            // Make sure the current user is the last editor:
            issue.LastEditedBy = currentUser.UserId;

            // Make sure the current user is the creator:
            issue.CreatedBy = currentUser.UserId;

            // Add the new Task, the HiLo Pattern automatically assigns a new Id using the HiLo Pattern
            await applicationDbContext
                .AddAsync(issue, cancellationToken)
                .ConfigureAwait(false);

            var userIssueRole = new UserIssueRole
            {
                UserId = currentUser.UserId,
                IssueId = issue.Id,
                Role = IssueRoleEnum.Creator,
                LastEditedBy = currentUser.UserId
            };

            var outboxEvent = OutboxEventUtils.Create(new IssueCreatedMessage
            {
                IssueId = issue.Id,
                RepositoryId = issue.RepositoryId,
                UserIssueRoles = new[] { userIssueRole }
                    .Select(x => new AddedUserToIssueMessage { UserId = x.UserId, IssueId = x.IssueId, Role = x.Role })
                    .ToList()
            }, lastEditedBy: currentUser.UserId);

            await applicationDbContext
                .AddAsync(outboxEvent, cancellationToken)
                .ConfigureAwait(false);

            await applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return issue;
        }

        public async Task<Issue> GetIssueByIdAsync(int issueId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var issue = await applicationDbContext.Issues.AsNoTracking()
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
                .CheckUserObjectAsync<Repository>(currentUser.UserId, issue.RepositoryId, RepositoryRoleEnum.Reader, cancellationToken)
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

        public async Task<List<Issue>> GetIssuesByRepositoryIdAsync(int repositoryId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, repositoryId, RepositoryRoleEnum.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = repositoryId,
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var issues = await applicationDbContext.Issues
                .AsNoTracking()
                .Where(x => x.RepositoryId == repositoryId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return issues;
        }

        public async Task<List<Issue>> GetIssuesByOrganizationIdAsync(int organizationId, CurrentUser currentUser, CancellationToken cancellationToken)
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

            var query = from organization in applicationDbContext.Organizations
                        join repository in applicationDbContext.Repositories on organization.Id equals repository.OrganizationId
                        join issue in applicationDbContext.Issues on repository.Id equals issue.RepositoryId
                        where organization.Id.Equals(organizationId)
                        select issue;

            var issues = await query.AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return issues;
        }

        public async Task<List<Issue>> GetIssuesAsync(CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            // Get all Repositories:
            var allRepositories = await applicationDbContext.Repositories.AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Get all Repositories we are Readers of:
            var repositories = await _aclService
                .CheckUserObjectsParallelAsync<Repository>(currentUser.UserId, allRepositories, RepositoryRoleEnum.Reader.AsRelation(), cancellationToken)
                .ConfigureAwait(false);

            // Get the IDs of allowed Repositories:
            var allowedRepositoryIds = repositories
                .Where(x => x.Allowed)
                .Select(x => x.Object.Id)
                .ToList();

            // Load the issues for all allowed Repositories:
            var issues = await applicationDbContext.Issues.AsNoTracking()
                .Where(x => allowedRepositoryIds.Contains(x.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return issues;
        }

        public async Task<Issue> UpdateIssueAsync(int issueId, Issue values, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var original = await applicationDbContext.Issues.AsNoTracking()
                .Where(x => x.Id == issueId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (original == null)
            {
                throw new EntityNotFoundException
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Repository>(currentUser.UserId, original.RepositoryId, RepositoryRoleEnum.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            int rowsAffected = await applicationDbContext.Issues
                .Where(t => t.Id == issueId && t.RowVersion == values.RowVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Title, values.Title)
                    .SetProperty(x => x.Content, values.Content)
                    .SetProperty(x => x.Closed, values.Closed)
                    .SetProperty(x => x.LastEditedBy, currentUser.UserId), cancellationToken)
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new EntityConcurrencyException()
                {
                    EntityName = nameof(Issue),
                    EntityId = values.Id,
                };
            }

            var updated = await applicationDbContext.Issues.AsNoTracking()
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

        public async Task DeleteIssueAsync(int issueId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isReadAuthorized = await _aclService
                .CheckUserObjectAsync<Issue>(currentUser.UserId, issueId, Relations.Reader, cancellationToken)
                .ConfigureAwait(false);

            if (!isReadAuthorized)
            {
                throw new EntityNotFoundException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var issue = await applicationDbContext.Issues.AsNoTracking()
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
                .CheckUserObjectAsync<Issue>(currentUser.UserId, issueId, Relations.Writer, cancellationToken)
                .ConfigureAwait(false);

            if (!isDeleteAuthorized)
            {
                throw new EntityUnauthorizedAccessException()
                {
                    EntityName = nameof(Issue),
                    EntityId = issueId,
                    UserId = currentUser.UserId,
                };
            }

            using (var transaction = await applicationDbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                var userIssueRoles = await applicationDbContext.UserIssueRoles
                    .Where(x => x.IssueId == issue.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                await applicationDbContext.UserIssueRoles
                        .Where(t => t.Id == issue.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);

                await applicationDbContext.Issues
                        .Where(t => t.Id == issue.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);

                var outboxEvent = OutboxEventUtils.Create(new IssueDeletedMessage
                {
                    IssueId = issue.Id,
                    RepositoryId = issue.RepositoryId,
                    UserIssueRoles = userIssueRoles
                        .Select(x => new RemovedUserFromIssueMessage { UserId = x.UserId, IssueId = x.IssueId, Role = x.Role })
                        .ToList()

                }, lastEditedBy: currentUser.UserId);

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
