using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Core;

namespace TodoApp.Api
{
    public sealed class DomainExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<DomainExceptionHandler> _logger;

        public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var problem = ToProblemDetails(exception);

            Log(httpContext, exception, problem);

            problem.Instance = httpContext.Request.Path;

            // Gives support a handle to find the log line for a 500 without the response
            // saying anything about what went wrong.
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = problem.Status!.Value;

            // WriteAsJsonAsync assigns the content type itself and overwrites whatever was
            // set beforehand, so problem+json has to be passed to it rather than set above.
            await httpContext.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json",
                cancellationToken);

            return true;
        }

        private static ProblemDetails ToProblemDetails(Exception exception) => exception switch
        {
            ValidationException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Detail = exception.Message
            },
            NotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Detail = exception.Message
            },
            ConflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflicting state.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Detail = exception.Message
            },
            // Anything else is a bug on our side. The client gets one fixed sentence and
            // nothing else: no exception type, no message, no stack trace.
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Detail = "The request could not be completed."
            }
        };

        private void Log(HttpContext httpContext, Exception exception, ProblemDetails problem)
        {
            // A client sending a blank title is not a fault in this system, so it is logged
            // as a warning without a stack trace. Only the 500 path carries the exception.
            if (problem.Status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    exception,
                    "Unhandled failure for {Method} {Path}.",
                    httpContext.Request.Method,
                    httpContext.Request.Path);
            }
            else
            {
                _logger.LogWarning(
                    "Rejected {Method} {Path} with {StatusCode}: {Reason}",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    problem.Status,
                    exception.Message);
            }
        }
    }
}
