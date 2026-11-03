using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Http.ProblemDetails;

public static class ProblemDetailsServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocksProblemDetails(
        this IServiceCollection services,
        Action<ExceptionProblemDetailsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<ExceptionProblemDetailsOptions>();
        }

        services.AddSingleton<ProblemDetailsExceptionFilter>();
        return services;
    }
}
