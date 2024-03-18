// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Errors;

namespace GitClub.Infrastructure.Exceptions
{
    public class UserAlreadyAssignedToRepositoryException : ApplicationErrorException
    {
        /// <inheritdoc/>
        public override string ErrorCode => ErrorCodes.UserAlreadyAssignedToRepository;

        /// <inheritdoc/>
        public override string ErrorMessage => $"UserAlreadyAssignedToRepository (UserId = {UserId}, RepositoryId = {RepositoryId})";

        /// <inheritdoc/>
        public override int HttpStatusCode => StatusCodes.Status428PreconditionRequired;

        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public required int UserId { get; set; }

        /// <summary>
        /// Gets or sets the RepositoryId.
        /// </summary>
        public required int RepositoryId { get; set; }

        /// <summary>
        /// Creates a new <see cref="EntityNotFoundException"/>.
        /// </summary>
        /// <param name="message">Error Message</param>
        /// <param name="innerException">Reference to the Inner Exception</param>
        public UserAlreadyAssignedToRepositoryException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
