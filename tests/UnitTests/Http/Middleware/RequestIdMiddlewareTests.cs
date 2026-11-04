using BuildingBlocks.Http.Middleware;
using Microsoft.AspNetCore.Http;

namespace UnitTests.Http.Middleware;

public class RequestIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithoutHeader_GeneratesRequestId()
    {
        // Arrange
        string? observed = null;
        var middleware = new RequestIdMiddleware(context =>
        {
            observed = context.TraceIdentifier;
            return Task.CompletedTask;
        });
        var httpContext = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(observed));
        Assert.Equal(observed, httpContext.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_WithHeader_ReusesRequestId()
    {
        // Arrange
        const string incoming = "req-demo-123";
        string? observed = null;
        var middleware = new RequestIdMiddleware(context =>
        {
            observed = context.TraceIdentifier;
            return Task.CompletedTask;
        });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[RequestIdMiddleware.HeaderName] = incoming;

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.Equal(incoming, observed);
        Assert.Equal(incoming, httpContext.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
    }
}
