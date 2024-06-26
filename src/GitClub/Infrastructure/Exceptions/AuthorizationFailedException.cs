﻿// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Errors;

namespace GitClub.Infrastructure.Exceptions
{
    public class AuthorizationFailedException : ApplicationErrorException
    {
        /// <inheritdoc/>
        public override string ErrorCode => ErrorCodes.AuthorizationFailed;

        /// <inheritdoc/>
        public override string ErrorMessage => $"AuthorizationFailed";

        /// <inheritdoc/>
        public override int HttpStatusCode => StatusCodes.Status403Forbidden;

        /// <summary>
        /// Creates a new <see cref="AuthorizationFailedException"/>.
        /// </summary>
        /// <param name="message">Error Message</param>
        /// <param name="innerException">Reference to the Inner Exception</param>
        public AuthorizationFailedException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
