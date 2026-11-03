namespace BuildingBlocks.Messaging;

public interface IPubSubTransport
{
    Task PublishAsync(
        string topicId,
        string data,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken);

    IAsyncEnumerable<PubSubMessage> SubscribeAsync(
        string subscriptionId,
        CancellationToken cancellationToken);

    Task AcknowledgeAsync(
        string subscriptionId,
        string messageId,
        CancellationToken cancellationToken);

    Task NegativeAcknowledgeAsync(
        string subscriptionId,
        string messageId,
        CancellationToken cancellationToken);
}
