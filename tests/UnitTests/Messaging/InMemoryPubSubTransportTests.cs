using BuildingBlocks.Messaging;

namespace UnitTests.Messaging;

public class InMemoryPubSubTransportTests
{
    [Fact]
    public async Task SubscribeAsync_WhenSubscriptionNotBound_Throws()
    {
        // Arrange
        var transport = new InMemoryPubSubTransport();

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in transport.SubscribeAsync("missing", CancellationToken.None))
            {
            }
        });
    }

    [Fact]
    public async Task PublishAsync_DoesNotDeliverToUnrelatedSubscription()
    {
        // Arrange
        var transport = new InMemoryPubSubTransport();
        transport.BindSubscription("topic-a", "sub-a");
        transport.BindSubscription("topic-b", "sub-b");

        // Act
        await transport.PublishAsync(
            "topic-a",
            "payload",
            new Dictionary<string, string>(),
            CancellationToken.None);

        // Assert
        Assert.Single(transport.GetPublished("topic-a"));
        Assert.Empty(transport.GetPublished("topic-b"));
        Assert.Empty(transport.GetPublished("topic-unbound"));
    }

    [Fact]
    public async Task SubscribeAsync_CompletesWhenChannelIsCompleted()
    {
        // Arrange
        var transport = new InMemoryPubSubTransport();
        transport.BindSubscription("topic-a", "sub-complete");

        // Act
        await transport.PublishAsync(
            "topic-a",
            "payload",
            new Dictionary<string, string>(),
            CancellationToken.None);
        CompleteSubscription(transport, "sub-complete");

        var messages = new List<PubSubMessage>();
        await foreach (var message in transport.SubscribeAsync("sub-complete", CancellationToken.None))
        {
            messages.Add(message);
        }

        // Assert
        Assert.Single(messages);
        Assert.Equal("payload", messages[0].Data);
    }

    private static void CompleteSubscription(InMemoryPubSubTransport transport, string subscriptionId)
    {
        var field = typeof(InMemoryPubSubTransport)
            .GetField(
                "_subscriptions",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        var subscriptions = (System.Collections.Concurrent.ConcurrentDictionary<
            string,
            System.Threading.Channels.Channel<PubSubMessage>>)field.GetValue(transport)!;
        Assert.True(subscriptions.TryGetValue(subscriptionId, out var channel));
        channel.Writer.TryComplete();
    }
}
