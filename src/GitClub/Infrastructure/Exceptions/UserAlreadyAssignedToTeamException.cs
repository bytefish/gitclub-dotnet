// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Errors;

namespace GitClub.Infrastructure.Exceptions
{
    public class UserAlreadyAssignedToTeamException : ApplicationErrorException
    {
        /// <inheritdoc/>
        public override string ErrorCode => ErrorCodes.UserAlreadyAssignedToTeam;

        /// <inheritdoc/>
        public override string ErrorMessage => $"UserAlreadyAssignedToTeam (UserId = {UserId}, TeamId = {TeamId})";

        /// <inheritdoc/>
        public override int HttpStatusCode => StatusCodes.Status428PreconditionRequired;

        /// <summary>
        /// Gets or sets the EntityId.
        /// </summary>
        public required int UserId { get; set; }
        
        /// <summary>
        /// Gets or sets the EntityId.
        /// </summary>
        public required int TeamId { get; set; }

        /// <summary>
        /// Creates a new <see cref="UserAlreadyAssignedToTeamException"/>.
        /// </summary>
        /// <param name="message">Error Message</param>
        /// <param name="innerException">Reference to the Inner Exception</param>
        public UserAlreadyAssignedToTeamException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
