// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Errors;
using GitClub.Infrastructure.Logging;
using GitClub.Models;
using Microsoft.Extensions.Options;
using RebacExperiments.Server.Api.Infrastructure.Exceptions;

namespace RebacExperiments.Server.Api.Infrastructure.Errors
{
    /// <summary>
    /// Handles errors returned by the application.
    /// </summary>
    public class ExceptionToApplicationErrorMapper
    {
        private readonly ILogger<ExceptionToApplicationErrorMapper> _logger;

        private readonly ExceptionToApplicationErrorMapperOptions _options;
        private readonly Dictionary<Type, IExceptionTranslator> _translators;

        public ExceptionToApplicationErrorMapper(ILogger<ExceptionToApplicationErrorMapper> logger, IOptions<ExceptionToApplicationErrorMapperOptions> options, IEnumerable<IExceptionTranslator> translators)
        {
            _logger = logger;
            _options = options.Value;
            _translators = translators.ToDictionary(x => x.ExceptionType, x => x);
        }

        public ApplicationErrorResult CreateApplicationErrorResult(HttpContext httpContext, Exception exception)
        {
            _logger.TraceMethodEntry();

            _logger.LogError(exception, "Call to '{RequestPath}' failed due to an Exception", httpContext.Request.Path);

            // Get the best matching translator for the exception ...
            var translator = GetTranslator(exception);

            // ... translate it to the Result ...
            var error = translator.GetApplicationErrorResult(exception, _options.IncludeExceptionDetails);

            // ... add error metadata, such as a Trace ID, ...
            AddMetadata(httpContext, error);

            // ... and return it.
            return error;
        }

        private void AddMetadata(HttpContext httpContext, ApplicationErrorResult result)
        {
            if(result.Error.InnerError == null)
            {
                result.Error.InnerError = new ApplicationInnerError();
            }

            result.Error.InnerError.AdditionalProperties["trace-id"] = httpContext.TraceIdentifier;
        }

        private IExceptionTranslator GetTranslator(Exception e)
        {
            if (e is ApplicationErrorException)
            {
                if (_translators.TryGetValue(e.GetType(), out var translator))
                {
                    return translator;
                }

                return _translators[typeof(ApplicationErrorException)];
            }

            return _translators[typeof(Exception)];
        }
    }
}