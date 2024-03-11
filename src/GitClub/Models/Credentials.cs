﻿namespace GitClub.Models
{
    /// <summary>
    /// Fake "Credentials".
    /// </summary>
    public class Credentials
    {
        /// <summary>
        /// Gets or sets the Email.
        /// </summary>
        public required string Email { get; set; }

        /// <summary>
        /// Gets or sets the list of roles for the user.
        /// </summary>
        public string[] Roles { get; set; } = [];
    }
}
