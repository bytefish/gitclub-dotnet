// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Errors;

namespace GitClub.Infrastructure.Exceptions
{
    public class UserNotAssignedToRepositoryException : ApplicationErrorException
    {
        /// <inheritdoc/>
        public override string ErrorCode => ErrorCodes.UserNotAssignedToRepository;

        /// <inheritdoc/>
        public override string ErrorMessage => $"UserNotAssignedToRepository (UserId = {UserId}, RepositoryId = {RepositoryId})";

        /// <inheritdoc/>
        public override int HttpStatusCode => StatusCodes.Status428PreconditionRequired;

        /// <summary>
        /// Gets or sets the EntityId.
        /// </summary>
        public required int UserId { get; set; }
        
        /// <summary>
        /// Gets or sets the EntityId.
        /// </summary>
        public required int RepositoryId { get; set; }

        /// <summary>
        /// Creates a new <see cref="UserNotAssignedToRepositoryException"/>.
        /// </summary>
        /// <param name="message">Error Message</param>
        /// <param name="innerException">Reference to the Inner Exception</param>
        public UserNotAssignedToRepositoryException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
