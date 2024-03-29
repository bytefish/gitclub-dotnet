// Licensed under the MIT license. See LICENSE file in the project root for full license information.
 
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Postgres
{
    /// <summary>
    /// Operation performed by Postgres.
    /// </summary>
    public enum PostgresOperationEnum
    {
        Insert = 1,
        Update = 2,
        Delete = 3
    }

    /// <summary>
    /// The Json Notification.
    /// </summary>
    public class PostgresJsonNotification
    {
        [JsonPropertyName("timestamp")]
        public required DateTime Timestamp { get; set; }

        [JsonPropertyName("schema")]
        public required string Schema { get; set; }

        [JsonPropertyName("operation")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required PostgresOperationEnum Operation { get; set; }

        [JsonPropertyName("table")]
        public required string Table { get; set; }

        [JsonPropertyName("payload")]
        public required JsonDocument Payload { get; set; }
    }
}
