using BuildingBlocks.Tracing;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocksMessaging(
        this IServiceCollection services,
        Action<PubSubOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<PubSubOptions>();
        }

        services.AddSingleton<IMessageTracePropagator, DatadogMessageTracePropagator>();
        services.AddSingleton<IPubSubTransport, GooglePubSubTransport>();
        services.AddSingleton<IEventPublisher, EventPublisher>();
        return services;
    }

    public static IServiceCollection AddEventSubscriptionWorker<TData, THandler>(
        this IServiceCollection services,
        string subscriptionId)
        where THandler : class, IIntegrationEventConsumer<TData>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        services.AddSingleton<IIntegrationEventConsumer<TData>, THandler>();
        services.AddHostedService(sp =>
        {
            var transport = sp.GetRequiredService<IPubSubTransport>();
            var handler = sp.GetRequiredService<IIntegrationEventConsumer<TData>>();
            var tracePropagator = sp.GetRequiredService<IMessageTracePropagator>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EventSubscriptionWorker<TData>>>();
            return new EventSubscriptionWorker<TData>(
                transport,
                handler,
                tracePropagator,
                subscriptionId,
                logger);
        });

        return services;
    }
}
