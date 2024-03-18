// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Errors;

namespace GitClub.Infrastructure.Exceptions
{
    public class TeamAlreadyAssignedToOrganizationException : ApplicationErrorException
    {
        /// <inheritdoc/>
        public override string ErrorCode => ErrorCodes.TeamAlreadyAssignedToOrganization;

        /// <inheritdoc/>
        public override string ErrorMessage => $"TeamAlreadyAssignedToOrganization (TeamId = {TeamId}, OrganizationId = {OrganizationId})";

        /// <inheritdoc/>
        public override int HttpStatusCode => StatusCodes.Status428PreconditionRequired;

        /// <summary>
        /// Gets or sets the EntityId.
        /// </summary>
        public required int OrganizationId { get; set; }
        
        /// <summary>
        /// Gets or sets the EntityId.
        /// </summary>
        public required int TeamId { get; set; }

        /// <summary>
        /// Creates a new <see cref="TeamAlreadyAssignedToOrganizationException"/>.
        /// </summary>
        /// <param name="message">Error Message</param>
        /// <param name="innerException">Reference to the Inner Exception</param>
        public TeamAlreadyAssignedToOrganizationException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
