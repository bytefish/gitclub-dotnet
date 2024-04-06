using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.OpenFga;
using GitClub.Infrastructure.Outbox.Messages;
using GitClub.Models;
using GitClub.Services;
using System.Reflection.Metadata.Ecma335;

namespace GitClub.Infrastructure.Outbox.Consumer
{
    public class OpenFgaOutboxEventConsumer
    {
        private readonly ILogger<OpenFgaOutboxEventConsumer> _logger;
        private readonly AclService _aclService;

        public OpenFgaOutboxEventConsumer(ILogger<OpenFgaOutboxEventConsumer> logger, AclService aclService)
        {
            _logger = logger;
            _aclService = aclService;
        }

        private async Task HandleOutboxEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
        {
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
                    await HandleRemovedUserFromRepoAsync(removedUserFromRepositoryMessage, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        private async Task HandleOrganizationCreatedAsync(OrganizationCreatedMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleOrganizationUpdatedAsync(OrganizationUpdatedMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleOrganizationDeletedAsync(OrganizationDeletedMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleTeamCreatedAsync(TeamCreatedMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleTeamDeletedAsync(TeamDeletedMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleTeamUpdatedAsync(TeamUpdatedMessage message, CancellationToken cancellationToken)
        {

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

            return Task.CompletedTask;
        }

        private async Task HandleRepositoryDeletedAsync(RepositoryDeletedMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleIssueCreatedAsync(IssueCreatedMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleIssueUpdatedAsync(IssueUpdatedMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleIssueDeletedAsync(IssueDeletedMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleAddedUserToOrganizationAsync(AddedUserToOrganizationMessage message, CancellationToken cancellationToken)
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

        private async Task HandleAddedUserToTeamAsync(AddedUserToTeamMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleAddedUserToRepositoryAsync(AddedUserToRepositoryMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleRemovedUserFromOrganizationAsync(RemovedUserFromOrganizationMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleRemovedUserFromTeamAsync(RemovedUserFromTeamMessage message, CancellationToken cancellationToken)
        {

        }

        private async Task HandleRemovedUserFromRepoAsync(RemovedUserFromRepositoryMessage message, CancellationToken cancellationToken)
        {

        }
    }
}
