// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using System.Text.Json;

namespace GitClub.Infrastructure.Outbox
{
    public static class OutboxEventExtensions
    {
        public static bool TryGetOutboxEventPayload(OutboxEvent outboxEvent, out object? result)
        {
            result = null;

            // Maybe throw here? We should probably log it at least...
            var type = Type.GetType(outboxEvent.EventType, throwOnError: false);

            if(type == null)
            {
                return false;
            }

            result = JsonSerializer.Deserialize(outboxEvent.Payload, type);

            return true;
        }
    }
}
