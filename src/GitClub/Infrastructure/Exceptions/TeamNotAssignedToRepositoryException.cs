// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Errors;

namespace GitClub.Infrastructure.Exceptions
{
    public class TeamNotAssignedToRepositoryException : ApplicationErrorException
    {
        /// <inheritdoc/>
        public override string ErrorCode => ErrorCodes.TeamNotAssignedToRepository;

        /// <inheritdoc/>
        public override string ErrorMessage => $"TeamNotAssignedToRepository (TeamId = {TeamId}, RepositoryId = {RepositoryId})";

        /// <inheritdoc/>
        public override int HttpStatusCode => StatusCodes.Status428PreconditionRequired;

        /// <summary>
        /// Gets or sets the EntityId.
        /// </summary>
        public required int TeamId { get; set; }
        
        /// <summary>
        /// Gets or sets the EntityId.
        /// </summary>
        public required int RepositoryId { get; set; }

        /// <summary>
        /// Creates a new <see cref="TeamNotAssignedToRepositoryException"/>.
        /// </summary>
        /// <param name="message">Error Message</param>
        /// <param name="innerException">Reference to the Inner Exception</param>
        public TeamNotAssignedToRepositoryException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
