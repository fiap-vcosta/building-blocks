using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Messaging;

[ExcludeFromCodeCoverage]
public sealed class GooglePubSubTransport(IOptions<PubSubOptions> options) : IPubSubTransport
{
    private readonly string _projectId = string.IsNullOrWhiteSpace(options.Value.ProjectId)
        ? throw new InvalidOperationException("PubSub:ProjectId is required.")
        : options.Value.ProjectId;

    private readonly ConcurrentDictionary<string, string> _ackIds = new(StringComparer.Ordinal);
    private PublisherServiceApiClient? _publisherApi;
    private SubscriberServiceApiClient? _subscriberApi;

    public async Task PublishAsync(
        string topicId,
        string data,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicId);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(attributes);

        var client = await GetPublisherApiAsync(cancellationToken);
        var topicName = TopicName.FromProjectTopic(_projectId, topicId);
        var message = new PubsubMessage
        {
            Data = ByteString.CopyFrom(data, System.Text.Encoding.UTF8)
        };
        foreach (var (key, value) in attributes)
        {
            message.Attributes[key] = value;
        }

        await client.PublishAsync(topicName, [message], cancellationToken);
    }

    public async IAsyncEnumerable<PubSubMessage> SubscribeAsync(
        string subscriptionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        var client = await GetSubscriberApiAsync(cancellationToken);
        var subscriptionName = SubscriptionName.FromProjectSubscription(_projectId, subscriptionId);

        while (!cancellationToken.IsCancellationRequested)
        {
            PullResponse response;
            try
            {
                response = await client.PullAsync(
                    subscriptionName,
                    maxMessages: 10,
                    callSettings: null);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (response.ReceivedMessages.Count == 0)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }

                continue;
            }

            foreach (var received in response.ReceivedMessages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var messageId = received.Message.MessageId;
                _ackIds[$"{subscriptionId}:{messageId}"] = received.AckId;

                yield return new PubSubMessage
                {
                    Id = messageId,
                    Data = received.Message.Data.ToStringUtf8(),
                    Attributes = new Dictionary<string, string>(
                        received.Message.Attributes,
                        StringComparer.Ordinal)
                };
            }
        }
    }

    public async Task AcknowledgeAsync(
        string subscriptionId,
        string messageId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (!_ackIds.TryRemove($"{subscriptionId}:{messageId}", out var ackId))
        {
            return;
        }

        var client = await GetSubscriberApiAsync(cancellationToken);
        var subscriptionName = SubscriptionName.FromProjectSubscription(_projectId, subscriptionId);
        await client.AcknowledgeAsync(subscriptionName, [ackId], cancellationToken);
    }

    public async Task NegativeAcknowledgeAsync(
        string subscriptionId,
        string messageId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (!_ackIds.TryRemove($"{subscriptionId}:{messageId}", out var ackId))
        {
            return;
        }

        var client = await GetSubscriberApiAsync(cancellationToken);
        var subscriptionName = SubscriptionName.FromProjectSubscription(_projectId, subscriptionId);
        await client.ModifyAckDeadlineAsync(subscriptionName, [ackId], 0, cancellationToken);
    }

    private async Task<PublisherServiceApiClient> GetPublisherApiAsync(CancellationToken cancellationToken)
    {
        if (_publisherApi is not null)
        {
            return _publisherApi;
        }

        _publisherApi = await PublisherServiceApiClient.CreateAsync(cancellationToken);
        return _publisherApi;
    }

    private async Task<SubscriberServiceApiClient> GetSubscriberApiAsync(CancellationToken cancellationToken)
    {
        if (_subscriberApi is not null)
        {
            return _subscriberApi;
        }

        _subscriberApi = await SubscriberServiceApiClient.CreateAsync(cancellationToken);
        return _subscriberApi;
    }
}
