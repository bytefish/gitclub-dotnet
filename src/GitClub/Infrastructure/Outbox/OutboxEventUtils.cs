using GitClub.Database.Models;
using System.Text.Json;

namespace GitClub.Infrastructure.Outbox
{
    public static class OutboxEventUtils
    {
        public static OutboxEvent Create<TMessageType>(TMessageType message, int lastEditedBy)
        {
            var outboxEvent = new OutboxEvent
            {
                EventType = typeof(TMessageType).FullName!,
                Payload = JsonSerializer.SerializeToDocument(message),
                LastEditedBy = lastEditedBy
            };

            return outboxEvent;
        }
    }
}
