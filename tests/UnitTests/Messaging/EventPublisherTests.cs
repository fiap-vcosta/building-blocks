using BuildingBlocks.Events;
using BuildingBlocks.Messaging;

namespace UnitTests.Messaging;

public class EventPublisherTests
{
    [Fact]
    public async Task PublishAsync_SerializesEnvelopeInjectsTraceAndSendsToTopic()
    {
        // Arrange
        var transport = new InMemoryPubSubTransport();
        var propagator = new RecordingTracePropagator();
        var publisher = new EventPublisher(transport, propagator);
        var data = new OrdemServicoFinalizadaData { OrdemServicoId = 10 };

        // Act
        await publisher.PublishAsync(
            TopicIds.OrdemServicoFinalizada,
            EventTypes.OrdemServicoFinalizada,
            data);

        // Assert
        var published = transport.GetPublished(TopicIds.OrdemServicoFinalizada);
        Assert.Single(published);
        var envelope = EventJson.Deserialize<OrdemServicoFinalizadaData>(published[0].Data);
        Assert.NotNull(envelope);
        Assert.Equal(EventTypes.OrdemServicoFinalizada, envelope.Type);
        Assert.Equal(10, envelope.Data.OrdemServicoId);
        Assert.Equal(EventTypes.OrdemServicoFinalizada, published[0].Attributes["eventType"]);
        Assert.Equal(1, propagator.InjectCalls);
        Assert.Equal("1", published[0].Attributes["x-datadog-trace-id"]);
    }

    [Fact]
    public async Task PublishAsync_RejectsEmptyTopicId()
    {
        // Arrange
        var publisher = new EventPublisher(new InMemoryPubSubTransport(), new RecordingTracePropagator());

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            publisher.PublishAsync(
                " ",
                EventTypes.OrdemServicoFinalizada,
                new OrdemServicoFinalizadaData { OrdemServicoId = 1 }));
    }

    [Fact]
    public async Task PublishAsync_RejectsEmptyEventType()
    {
        // Arrange
        var publisher = new EventPublisher(new InMemoryPubSubTransport(), new RecordingTracePropagator());

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            publisher.PublishAsync(
                TopicIds.OrdemServicoFinalizada,
                " ",
                new OrdemServicoFinalizadaData { OrdemServicoId = 1 }));
    }

    [Fact]
    public async Task PublishAsync_RejectsNullData()
    {
        // Arrange
        var publisher = new EventPublisher(new InMemoryPubSubTransport(), new RecordingTracePropagator());

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync<OrdemServicoFinalizadaData>(
                TopicIds.OrdemServicoFinalizada,
                EventTypes.OrdemServicoFinalizada,
                null!));
    }
}
