﻿// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace GitClub.Infrastructure.Messages
{
    public class IssueDeletedMessage
    {
        /// <summary>
        /// Gets or sets the Issue ID.
        /// </summary>
        [JsonPropertyName("issueId")]
        public required int IssueId { get; set; }
    }
}
