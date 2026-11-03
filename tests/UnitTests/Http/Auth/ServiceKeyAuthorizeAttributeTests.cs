using BuildingBlocks.Http.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Http.Auth;

public class ServiceKeyAuthorizeAttributeTests
{
    [Fact]
    public void OnAuthorization_SetsUnauthorized_WhenConfiguredKeyMissing()
    {
        // Arrange
        var context = CreateContext("provided", expectedKey: null);
        var filter = new ServiceKeyAuthorizeAttribute();

        // Act
        filter.OnAuthorization(context);

        // Assert
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_SetsUnauthorized_WhenHeaderMissing()
    {
        // Arrange
        var context = CreateContext(null, "expected-key");
        var filter = new ServiceKeyAuthorizeAttribute();

        // Act
        filter.OnAuthorization(context);

        // Assert
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_SetsUnauthorized_WhenKeyWrong()
    {
        // Arrange
        var context = CreateContext("wrong", "expected-key");
        var filter = new ServiceKeyAuthorizeAttribute();

        // Act
        filter.OnAuthorization(context);

        // Assert
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_SetsUnauthorized_WhenKeyLengthDiffers()
    {
        // Arrange
        var context = CreateContext("short", "expected-key");
        var filter = new ServiceKeyAuthorizeAttribute();

        // Act
        filter.OnAuthorization(context);

        // Assert
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_Allows_WhenKeyMatches()
    {
        // Arrange
        var context = CreateContext("expected-key", "expected-key");
        var filter = new ServiceKeyAuthorizeAttribute();

        // Act
        filter.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    private static AuthorizationFilterContext CreateContext(string? providedKey, string? expectedKey)
    {
        var values = new Dictionary<string, string?>();
        if (expectedKey is not null)
        {
            values["ServiceAuth:Key"] = expectedKey;
        }

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build());
        var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        if (providedKey is not null)
        {
            httpContext.Request.Headers[ServiceKeyAuthorizeAttribute.HeaderName] = providedKey;
        }

        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
