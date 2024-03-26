﻿// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Errors;

namespace GitClub.Infrastructure.Exceptions
{
    public class CannotDeleteOwnUserException : ApplicationErrorException
    {
        /// <inheritdoc/>
        public override string ErrorCode => ErrorCodes.CannotDeleteOwnUserException;

        /// <inheritdoc/>
        public override string ErrorMessage => $"CannotDeleteOwnUserException (UserId = {UserId})";

        /// <inheritdoc/>
        public override int HttpStatusCode => StatusCodes.Status428PreconditionRequired;

        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public required int UserId { get; set; }

        /// <summary>
        /// Creates a new <see cref="CannotDeleteOwnUserException"/>.
        /// </summary>
        /// <param name="message">Error Message</param>
        /// <param name="innerException">Reference to the Inner Exception</param>
        public CannotDeleteOwnUserException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
