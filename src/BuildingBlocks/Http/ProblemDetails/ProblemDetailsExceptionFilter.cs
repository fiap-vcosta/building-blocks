using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Http.ProblemDetails;

public sealed class ProblemDetailsExceptionFilter(IOptions<ExceptionProblemDetailsOptions> options) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled)
        {
            return;
        }

        var (statusCode, title, detail) = MapException(context.Exception);

        context.Result = new ObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        })
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }

    private (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        foreach (var (type, mapper) in options.Value.Mappings)
        {
            if (type.IsInstanceOfType(exception))
            {
                return mapper(exception);
            }
        }

        return exception switch
        {
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                exception.Message),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                exception.Message),
            InvalidOperationException or ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request.",
                "An unexpected error occurred.")
        };
    }
}
