﻿// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Messages
{
    public class RemovedUserFromRepositoryMessage
    {
        /// <summary>
        /// Gets or sets the User ID.
        /// </summary>
        [JsonPropertyName("userId")]
        public required int UserId { get; set; }

        /// <summary>
        /// Gets or sets the Repository ID.
        /// </summary>
        [JsonPropertyName("repositoryId")]
        public required int RepositoryId { get; set; }
    }
}