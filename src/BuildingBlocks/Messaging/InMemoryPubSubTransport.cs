using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace BuildingBlocks.Messaging;

public class InMemoryPubSubTransport : IPubSubTransport
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<PubSubMessage>> _topics =
        new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, Channel<PubSubMessage>> _subscriptions =
        new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, string> _subscriptionTopics =
        new(StringComparer.Ordinal);

    public void BindSubscription(string topicId, string subscriptionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        _topics.GetOrAdd(topicId, _ => []);
        _subscriptions.GetOrAdd(
            subscriptionId,
            _ => Channel.CreateUnbounded<PubSubMessage>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }));
        _subscriptionTopics[subscriptionId] = topicId;
    }

    public Task PublishAsync(
        string topicId,
        string data,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicId);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(attributes);

        var message = new PubSubMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            Data = data,
            Attributes = new Dictionary<string, string>(attributes, StringComparer.Ordinal)
        };

        var bag = _topics.GetOrAdd(topicId, _ => []);
        bag.Add(message);

        foreach (var (subscriptionId, boundTopic) in _subscriptionTopics)
        {
            if (!string.Equals(boundTopic, topicId, StringComparison.Ordinal))
            {
                continue;
            }

            if (_subscriptions.TryGetValue(subscriptionId, out var channel))
            {
                channel.Writer.TryWrite(Clone(message));
            }
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<PubSubMessage> SubscribeAsync(
        string subscriptionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        if (!_subscriptions.TryGetValue(subscriptionId, out var channel))
        {
            throw new InvalidOperationException($"Subscription '{subscriptionId}' is not bound.");
        }

        await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return message;
        }
    }

    public virtual Task AcknowledgeAsync(
        string subscriptionId,
        string messageId,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public virtual Task NegativeAcknowledgeAsync(
        string subscriptionId,
        string messageId,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public IReadOnlyList<PubSubMessage> GetPublished(string topicId) =>
        _topics.TryGetValue(topicId, out var bag)
            ? bag.ToArray()
            : [];

    private static PubSubMessage Clone(PubSubMessage message) =>
        new()
        {
            Id = message.Id,
            Data = message.Data,
            Attributes = new Dictionary<string, string>(message.Attributes, StringComparer.Ordinal)
        };
}
