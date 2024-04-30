// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Errors;

namespace GitClub.Infrastructure.Exceptions
{
    public class UserNotAssignedToOrganizationException : ApplicationErrorException
    {
        /// <inheritdoc/>
        public override string ErrorCode => ErrorCodes.UserNotAssignedToOrganization;

        /// <inheritdoc/>
        public override string ErrorMessage => $"UserNotAssignedToOrganizationInRoleException (UserId = {UserId}, OrganizationId = {OrganizationId}, Role = {Role})";

        /// <inheritdoc/>
        public override int HttpStatusCode => StatusCodes.Status428PreconditionRequired;

        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public required int UserId { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationId.
        /// </summary>
        public required int OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the Role.
        /// </summary>
        public required OrganizationRoleEnum Role { get; set; }

        /// <summary>
        /// Creates a new <see cref="UserNotAssignedToOrganizationException"/>.
        /// </summary>
        /// <param name="message">Error Message</param>
        /// <param name="innerException">Reference to the Inner Exception</param>
        public UserNotAssignedToOrganizationException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
