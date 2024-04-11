using GitClub.Database;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Outbox.Consumer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GitClub.Tests.Infrastructure
{
    public class OutboxEventProcessor
    {
        private readonly OutboxEventConsumer _outboxEventConsumer;
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        public OutboxEventProcessor(IDbContextFactory<ApplicationDbContext> dbContextFactory, OutboxEventConsumer outboxEventConsumer)
        {
            _dbContextFactory = dbContextFactory;
            _outboxEventConsumer = outboxEventConsumer;
        }

        public async Task ProcessAllOutboxEvents(CancellationToken cancellationToken)
        {

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var outboxEvents = await applicationDbContext.OutboxEvents.AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach(var outboxEvent in outboxEvents)
            {
                await _outboxEventConsumer
                    .ConsumeOutboxEventAsync(outboxEvent, cancellationToken)
                    .ConfigureAwait(false);

                await applicationDbContext.OutboxEvents
                    .Where(x => x.Id == outboxEvent.Id)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
