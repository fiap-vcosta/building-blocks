using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Http.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ServiceKeyAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Service-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expected = configuration["ServiceAuth:Key"];

        if (string.IsNullOrWhiteSpace(expected))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var provided) ||
            string.IsNullOrWhiteSpace(provided))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());

        if (expectedBytes.Length != providedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
