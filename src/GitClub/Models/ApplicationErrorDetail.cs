﻿using System.Text.Json.Serialization;

namespace GitClub.Models
{
    /// <summary>
    /// Represents an error detail.
    /// </summary>
    public class ApplicationErrorDetail
    {

        /// <summary>
        /// The value for the code name/value pair is a non-empty language-independent string. Its value is a 
        /// service-defined error code.This code serves as a sub-status for the HTTP error code specified in 
        /// the response.It cannot be null.
        /// </summary>
        public required string ErrorCode { get; set; }

        /// <summary>
        /// The value for the message name/value pair is a non-empty, language-dependent, human-readable string describing 
        /// the error.The Content-Language header MUST contain the language code from [RFC5646] corresponding to the language 
        /// in which the value for message is written.It cannot be null.
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// The value for the target name/value pair is a potentially empty string indicating the target of the error (for 
        /// example, the name of the property in error). It can be null.
        /// </summary>
        public required string Target { get; set; }

        /// <summary>
        /// Additional Properties.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    }
}