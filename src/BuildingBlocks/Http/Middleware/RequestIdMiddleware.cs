using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace BuildingBlocks.Http.Middleware;

public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Request-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(requestId))
        {
            requestId = Guid.NewGuid().ToString("N");
        }

        context.TraceIdentifier = requestId;
        context.Response.Headers[HeaderName] = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        {
            await next(context);
        }
    }
}
