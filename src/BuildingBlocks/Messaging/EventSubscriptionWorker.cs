using BuildingBlocks.Events;
using BuildingBlocks.Tracing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Messaging;

public sealed partial class EventSubscriptionWorker<TData>(
    IPubSubTransport transport,
    IIntegrationEventConsumer<TData> handler,
    IMessageTracePropagator tracePropagator,
    string subscriptionId,
    ILogger<EventSubscriptionWorker<TData>> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in transport.SubscribeAsync(subscriptionId, stoppingToken))
        {
            try
            {
                await ProcessMessageAsync(message, stoppingToken);
                await transport.AcknowledgeAsync(subscriptionId, message.Id, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogProcessingFailed(logger, message.Id, subscriptionId, ex);
                await transport.NegativeAcknowledgeAsync(subscriptionId, message.Id, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(PubSubMessage message, CancellationToken cancellationToken)
    {
        using var scope = tracePropagator.StartConsumerScope(
            $"pubsub.consume {handler.EventType}",
            message.Attributes);

        var envelope = EventJson.Deserialize<TData>(message.Data)
            ?? throw new InvalidOperationException("Pub/Sub message body is not a valid event envelope.");

        if (!string.Equals(envelope.Type, handler.EventType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Event type '{envelope.Type}' does not match handler '{handler.EventType}'.");
        }

        await handler.HandleAsync(envelope, cancellationToken);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Failed to process Pub/Sub message {MessageId} on {SubscriptionId}")]
    private static partial void LogProcessingFailed(
        ILogger logger,
        string messageId,
        string subscriptionId,
        Exception exception);
}
