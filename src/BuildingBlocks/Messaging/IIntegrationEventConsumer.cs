using BuildingBlocks.Events;

namespace BuildingBlocks.Messaging;

public interface IIntegrationEventConsumer<TData>
{
    string EventType { get; }

    Task HandleAsync(EventEnvelope<TData> envelope, CancellationToken cancellationToken);
}
