using BuildingBlocks.Events;
using BuildingBlocks.Tracing;

namespace BuildingBlocks.Messaging;

public sealed class EventPublisher(
    IPubSubTransport transport,
    IMessageTracePropagator tracePropagator) : IEventPublisher
{
    public Task PublishAsync<TData>(
        string topicId,
        string eventType,
        TData data,
        CancellationToken cancellationToken = default) =>
        PublishAsync(topicId, eventType, data, version: 1, cancellationToken);

    public async Task PublishAsync<TData>(
        string topicId,
        string eventType,
        TData data,
        int version,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);

        var envelope = new EventEnvelope<TData>
        {
            EventId = Guid.NewGuid(),
            Type = eventType,
            Version = version,
            OccurredAt = DateTimeOffset.UtcNow,
            Data = data
        };

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["eventType"] = eventType
        };
        tracePropagator.Inject(attributes);

        await transport.PublishAsync(
            topicId,
            EventJson.Serialize(envelope),
            attributes,
            cancellationToken);
    }
}
