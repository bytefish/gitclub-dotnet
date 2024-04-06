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

        public static bool TryGetOutboxEventPayload(OutboxEvent outboxEvent, out object? result)
        {
            result = null;

            // Maybe throw here? We should probably log it at least...
            var type = Type.GetType(outboxEvent.EventType, throwOnError: false);

            if (type == null)
            {
                return false;
            }

            result = JsonSerializer.Deserialize(outboxEvent.Payload, type);

            return true;
        }

    }
}
