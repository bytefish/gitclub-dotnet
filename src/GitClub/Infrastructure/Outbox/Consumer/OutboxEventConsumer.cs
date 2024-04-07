using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.OpenFga;
using GitClub.Infrastructure.Outbox.Messages;
using GitClub.Models;
using GitClub.Services;
using System.Reflection.Metadata.Ecma335;

namespace GitClub.Infrastructure.Outbox.Consumer
{
    public class OutboxEventConsumer
    {
        private readonly ILogger<OutboxEventConsumer> _logger;

        private readonly AclService _aclService;

        public OutboxEventConsumer(ILogger<OutboxEventConsumer> logger, AclService aclService)
        {
            _logger = logger;
            _aclService = aclService;
        }

        public async Task HandleOutboxEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var success = OutboxEventUtils.TryGetOutboxEventPayload(outboxEvent, out object? payload);

            // Maybe it's better to throw up, if we receive an event, we can't handle? But probably 
            // this wasn't meant for our Service at all? We don't know, so we log a Warning and go 
            // on with life ...
            if (!success)
            {
                _logger.LogWarning("Failed to get Data from OutboxEvent");

                return;
            }

            // Now handle the given payload ...
            switch (payload)
            {
                case OrganizationCreatedMessage organizationCreatedMessage:
                    await HandleOrganizationCreatedAsync(organizationCreatedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case OrganizationUpdatedMessage organizationUpdatedMessage:
                    await HandleOrganizationUpdatedAsync(organizationUpdatedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case OrganizationDeletedMessage organizationDeletedMessage:
                    await HandleOrganizationDeletedAsync(organizationDeletedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case TeamCreatedMessage teamCreatedMessage:
                    await HandleTeamCreatedAsync(teamCreatedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case TeamUpdatedMessage TeamUpdatedMessage:
                    await HandleTeamUpdatedAsync(TeamUpdatedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case TeamDeletedMessage teamDeletedMessage:
                    await HandleTeamDeletedAsync(teamDeletedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case RepositoryCreatedMessage repositoryCreatedMessage:
                    await HandleRepositoryCreatedAsync(repositoryCreatedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case RepositoryUpdatedMessage repositoryUpdatedMessage:
                    await HandleRepositoryUpdatedAsync(repositoryUpdatedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case RepositoryDeletedMessage repositoryDeletedMessage:
                    await HandleRepositoryDeletedAsync(repositoryDeletedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case IssueCreatedMessage issueCreatedMessage:
                    await HandleIssueCreatedAsync(issueCreatedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case IssueUpdatedMessage issueUpdatedMessage:
                    await HandleIssueUpdatedAsync(issueUpdatedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case IssueDeletedMessage issueDeletedMessage:
                    await HandleIssueDeletedAsync(issueDeletedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case UserDeletedMessage userDeletedMessage:
                    await HandleUserDeletedAsync(userDeletedMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case AddedUserToOrganizationMessage addedUserToOrganizationMessage:
                    await HandleAddedUserToOrganizationAsync(addedUserToOrganizationMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case AddedUserToTeamMessage addedUserToTeamMessage:
                    await HandleAddedUserToTeamAsync(addedUserToTeamMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case AddedUserToRepositoryMessage addedUserToRepositoryMessage:
                    await HandleAddedUserToRepositoryAsync(addedUserToRepositoryMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case RemovedUserFromOrganizationMessage removedUserFromOrganizationMessage:
                    await HandleRemovedUserFromOrganizationAsync(removedUserFromOrganizationMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case RemovedUserFromTeamMessage removedUserFromTeamMessage:
                    await HandleRemovedUserFromTeamAsync(removedUserFromTeamMessage, cancellationToken).ConfigureAwait(false);
                    break;
                case RemovedUserFromRepositoryMessage removedUserFromRepositoryMessage:
                    await HandleRemovedUserFromRepositoryAsync(removedUserFromRepositoryMessage, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        private async Task HandleOrganizationCreatedAsync(OrganizationCreatedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToWrite =
            [
                RelationTuples.Create<Organization, Organization>(message.OrganizationId, message.OrganizationId, message.BaseRepositoryRole, Relations.Member),
                ..message.UserOrganizationRoles
                    .Select(x => RelationTuples.Create<Organization, User>(x.OrganizationId, x.UserId, x.Role))
                    .ToArray()                
            ];

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);

        }

        private async Task HandleOrganizationUpdatedAsync(OrganizationUpdatedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToWrite =
            [
                RelationTuples.Create<Organization, Organization>(message.OrganizationId, message.OrganizationId, message.NewBaseRepositoryRole, Relations.Member)
            ];

            RelationTuple[] tuplesToDelete =
            [
                RelationTuples.Create<Organization, Organization>(message.OrganizationId, message.OrganizationId, message.OldBaseRepositoryRole, Relations.Member)
            ];

            await _aclService
                .WriteAsync(tuplesToWrite, tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        private Task HandleOrganizationDeletedAsync(OrganizationDeletedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // We do not support it right now, all Relations still hold...

            return Task.CompletedTask;
        }

        private async Task HandleTeamCreatedAsync(TeamCreatedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToDelete =
            [
                ..message.UserTeamRoles
                    .Select(x => RelationTuples.Create<Team, User>(x.TeamId, x.UserId, x.Role))
                    .ToArray()
            ];

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HandleTeamDeletedAsync(TeamDeletedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToDelete =
            [
                ..message.UserTeamRoles
                    .Select(x => RelationTuples.Create<Team, User>(x.TeamId, x.UserId, x.Role))
                    .ToArray()
            ];

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        private Task HandleTeamUpdatedAsync(TeamUpdatedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // Nothing to do, all Relations still hold...

            return Task.CompletedTask;
        }

        private async Task HandleRepositoryCreatedAsync(RepositoryCreatedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToWrite =
            [
                RelationTuples.Create<Repository, Organization>(message.RepositoryId, message.OrganizationId, RepositoryRoleEnum.Owner),
                ..message.UserRepositoryRoles
                    .Select(x => RelationTuples.Create<Repository, User>(x.RepositoryId, x.UserId, x.Role))
                    .ToArray()
            ];

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);
        }

        private Task HandleRepositoryUpdatedAsync(RepositoryUpdatedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // The Relations do not change ...

            return Task.CompletedTask;
        }

        private async Task HandleRepositoryDeletedAsync(RepositoryDeletedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToDelete = 
            [
                ..message.UserRepositoryRoles
                    .Select(x => RelationTuples.Create<Repository, User>(x.RepositoryId, x.UserId, x.Role))
                    .ToArray(),
                ..message.TeamRepositoryRoles
                    .Select(x => RelationTuples.Create<Repository, Team>(x.RepositoryId, x.TeamId, x.Role))
                    .ToArray()
            ];

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HandleIssueCreatedAsync(IssueCreatedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToWrite =
            [
                RelationTuples.Create<Issue, Repository>(message.IssueId, message.RepositoryId, IssueRoleEnum.Owner),
                ..message.UserIssueRoles
                    .Select(x => RelationTuples.Create<Issue, User>(x.IssueId, x.UserId, x.Role))
                    .ToArray()
            ];
        
            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);
        }

        private Task HandleIssueUpdatedAsync(IssueUpdatedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            // The Relations do not change ...

            return Task.CompletedTask;
        }

        private async Task HandleIssueDeletedAsync(IssueDeletedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToDelete =
            [
                RelationTuples.Create<Issue, Repository>(message.IssueId, message.RepositoryId, IssueRoleEnum.Owner),
                ..message.UserIssueRoles
                    .Select(x => RelationTuples.Create<Issue, User>(x.IssueId, x.UserId, x.Role))
                    .ToArray()
            ];

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HandleUserDeletedAsync(UserDeletedMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToDelete =
            [
                ..message.UserIssueRoles
                    .Select(x => RelationTuples.Create<Issue, User>(x.IssueId, x.UserId, x.Role))
                    .ToArray(),
                ..message.UserTeamRoles
                    .Select(x => RelationTuples.Create<Team, User>(x.TeamId, x.UserId, x.Role))
                    .ToArray(),
                ..message.UserOrganizationRoles
                    .Select(x => RelationTuples.Create<Organization, User>(x.OrganizationId, x.UserId, x.Role))
                    .ToArray(),
             ];

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HandleAddedUserToOrganizationAsync(AddedUserToOrganizationMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToWrite =
            [
                RelationTuples.Create<Organization, User>(message.OrganizationId, message.UserId, message.Role)
            ];

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HandleAddedUserToTeamAsync(AddedUserToTeamMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToWrite =
            [
                RelationTuples.Create<Team, User>(message.TeamId, message.UserId, message.Role)
            ];

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HandleAddedUserToRepositoryAsync(AddedUserToRepositoryMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToWrite =
            [
                RelationTuples.Create<Repository, User>(message.RepositoryId, message.UserId, message.Role)
            ];

            await _aclService
                .AddRelationshipsAsync(tuplesToWrite, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HandleRemovedUserFromOrganizationAsync(RemovedUserFromOrganizationMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToDelete =
            [
                RelationTuples.Create<Organization, User>(message.OrganizationId, message.UserId, message.Role)
            ];

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HandleRemovedUserFromTeamAsync(RemovedUserFromTeamMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToDelete =
            [
                RelationTuples.Create<Team, User>(message.TeamId, message.UserId, message.Role)
            ];

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task HandleRemovedUserFromRepositoryAsync(RemovedUserFromRepositoryMessage message, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            RelationTuple[] tuplesToDelete =
            [
                RelationTuples.Create<Repository, User>(message.RepositoryId, message.UserId, message.Role)
            ];

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);

        }
    }
}
