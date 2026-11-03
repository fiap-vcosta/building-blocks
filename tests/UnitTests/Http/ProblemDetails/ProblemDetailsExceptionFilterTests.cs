using BuildingBlocks.Http.ProblemDetails;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace UnitTests.Http.ProblemDetails;

public class ProblemDetailsExceptionFilterTests
{
    [Fact]
    public void OnException_Returns404_WhenKeyNotFoundException()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateExceptionContext(new KeyNotFoundException("missing"));

        // Act
        filter.OnException(context);

        // Assert
        AssertProblem(context, StatusCodes.Status404NotFound, "Not Found", "missing");
    }

    [Fact]
    public void OnException_Returns401_WhenUnauthorizedAccessException()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateExceptionContext(new UnauthorizedAccessException("denied"));

        // Act
        filter.OnException(context);

        // Assert
        AssertProblem(context, StatusCodes.Status401Unauthorized, "Unauthorized", "denied");
    }

    [Fact]
    public void OnException_Returns400_WhenInvalidOperationException()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateExceptionContext(new InvalidOperationException("bad op"));

        // Act
        filter.OnException(context);

        // Assert
        AssertProblem(context, StatusCodes.Status400BadRequest, "Bad Request", "bad op");
    }

    [Fact]
    public void OnException_Returns400_WhenArgumentException()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateExceptionContext(new ArgumentException("bad arg"));

        // Act
        filter.OnException(context);

        // Assert
        AssertProblem(context, StatusCodes.Status400BadRequest, "Bad Request", "bad arg");
    }

    [Fact]
    public void OnException_Returns500_WhenUnhandledException()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateExceptionContext(new NotSupportedException("secret"));

        // Act
        filter.OnException(context);

        // Assert
        AssertProblem(
            context,
            StatusCodes.Status500InternalServerError,
            "An error occurred while processing your request.",
            "An unexpected error occurred.");
    }

    [Fact]
    public void OnException_UsesExtraMapping_WhenRegistered()
    {
        // Arrange
        var filter = CreateFilter(options =>
            options.Map<CustomDomainException>(StatusCodes.Status404NotFound, "Not Found"));
        var context = CreateExceptionContext(new CustomDomainException("domain missing"));

        // Act
        filter.OnException(context);

        // Assert
        AssertProblem(context, StatusCodes.Status404NotFound, "Not Found", "domain missing");
    }

    [Fact]
    public void OnException_DoesNothing_WhenAlreadyHandled()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateExceptionContext(new InvalidOperationException("x"));
        context.ExceptionHandled = true;

        // Act
        filter.OnException(context);

        // Assert
        Assert.Null(context.Result);
        Assert.True(context.ExceptionHandled);
    }

    private static ProblemDetailsExceptionFilter CreateFilter(
        Action<ExceptionProblemDetailsOptions>? configure = null)
    {
        var options = new ExceptionProblemDetailsOptions();
        configure?.Invoke(options);
        return new ProblemDetailsExceptionFilter(Options.Create(options));
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, [])
        {
            Exception = exception
        };
    }

    private static void AssertProblem(
        ExceptionContext context,
        int statusCode,
        string title,
        string detail)
    {
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(statusCode, result.StatusCode);
        var problem = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(result.Value);
        Assert.Equal(statusCode, problem.Status);
        Assert.Equal(title, problem.Title);
        Assert.Equal(detail, problem.Detail);
        Assert.True(context.ExceptionHandled);
    }

    private sealed class CustomDomainException(string message) : Exception(message);
}
