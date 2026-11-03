using BuildingBlocks.Events;

namespace BuildingBlocks.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TData>(
        string topicId,
        string eventType,
        TData data,
        CancellationToken cancellationToken = default);
}
