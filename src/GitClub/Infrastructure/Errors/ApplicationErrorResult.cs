using GitClub.Models;
using Microsoft.AspNetCore.Mvc;

namespace GitClub.Infrastructure.Errors
{
    /// <summary>
    /// Represents a result that when executed will produce an <see cref="ActionResult"/>.
    /// </summary>
    /// <remarks>This result creates an <see cref="ApplicationError"/> response.</remarks>
    public class ApplicationErrorResult : ActionResult
    {
        /// <summary>
        /// OData error.
        /// </summary>
        public required ApplicationError Error { get; set; }

        /// <summary>
        /// Http Status Code.
        /// </summary>
        public required int HttpStatusCode { get; set; }

        /// <inheritdoc/>
        public async override Task ExecuteResultAsync(ActionContext context)
        {
            ObjectResult objectResult = new ObjectResult(Error)
            {
                StatusCode = HttpStatusCode
            };

            await objectResult
                .ExecuteResultAsync(context)
                .ConfigureAwait(false);
        }
    }
}
