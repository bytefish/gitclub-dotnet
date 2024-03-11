﻿// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Logging;
using GitClub.Models;

namespace GitClub.Infrastructure.Errors.Translators
{
    public class DefaultErrorExceptionTranslator : IExceptionTranslator
    {
        private readonly ILogger<DefaultErrorExceptionTranslator> _logger;

        public DefaultErrorExceptionTranslator(ILogger<DefaultErrorExceptionTranslator> logger)
        {
            _logger = logger;
        }

        public Type ExceptionType => typeof(Exception);

        public ApplicationErrorResult GetApplicationErrorResult(Exception exception, bool includeExceptionDetails)
        {
            _logger.TraceMethodEntry();

            var error = new ApplicationError
            {
                Code = ErrorCodes.InternalServerError,
                Message = "An Internal Server Error occured"
            };

            // Create the Inner Error
            error.InnerError = new ApplicationInnerError();

            if (includeExceptionDetails)
            {
                error.InnerError.Message = exception.Message;
                error.InnerError.StackTrace = exception.StackTrace;
                error.InnerError.Target = exception.GetType().Name;
            }

            return new ApplicationErrorResult
            {
                Error = error,
                HttpStatusCode = StatusCodes.Status500InternalServerError,
            };
        }
    }
}
