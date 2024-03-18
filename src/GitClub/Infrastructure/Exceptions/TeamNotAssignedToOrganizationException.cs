// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Errors;

namespace GitClub.Infrastructure.Exceptions
{
    public class TeamNotAssignedToOrganizationException : ApplicationErrorException
    {
        /// <inheritdoc/>
        public override string ErrorCode => ErrorCodes.TeamNotAssignedToOrganization;

        /// <inheritdoc/>
        public override string ErrorMessage => $"TeamNotAssignedToOrganization (TeamId = {TeamId}, OrganizationId = {OrganizationId})";

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
        /// Creates a new <see cref="TeamNotAssignedToOrganizationException"/>.
        /// </summary>
        /// <param name="message">Error Message</param>
        /// <param name="innerException">Reference to the Inner Exception</param>
        public TeamNotAssignedToOrganizationException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
