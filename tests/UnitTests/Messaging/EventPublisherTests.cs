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
        Assert.Equal(1, envelope.Version);
        Assert.Equal(10, envelope.Data.OrdemServicoId);
        Assert.Equal(EventTypes.OrdemServicoFinalizada, published[0].Attributes["eventType"]);
        Assert.Equal(1, propagator.InjectCalls);
        Assert.Equal("1", published[0].Attributes["x-datadog-trace-id"]);
    }

    [Fact]
    public async Task PublishAsync_WithExplicitVersion_SetsEnvelopeVersion()
    {
        // Arrange
        var transport = new InMemoryPubSubTransport();
        var publisher = new EventPublisher(transport, new RecordingTracePropagator());
        var data = new DiagnosticoFinalizadoData
        {
            OrdemServicoId = 10,
            VersaoDiagnostico = 1,
            Cliente = new DiagnosticoFinalizadoCliente
            {
                Nome = "Ada",
                Email = "ada@oficina.dev",
                Documento = "86421537090"
            },
            Servicos = []
        };

        // Act
        await publisher.PublishAsync(
            TopicIds.DiagnosticoFinalizado,
            EventTypes.DiagnosticoFinalizado,
            data,
            version: 2);

        // Assert
        var published = transport.GetPublished(TopicIds.DiagnosticoFinalizado);
        Assert.Single(published);
        var envelope = EventJson.Deserialize<DiagnosticoFinalizadoData>(published[0].Data);
        Assert.NotNull(envelope);
        Assert.Equal(2, envelope.Version);
        Assert.Equal("Ada", envelope.Data.Cliente.Nome);
    }

    [Fact]
    public async Task PublishAsync_RejectsVersionBelowOne()
    {
        // Arrange
        var publisher = new EventPublisher(new InMemoryPubSubTransport(), new RecordingTracePropagator());

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            publisher.PublishAsync(
                TopicIds.OrdemServicoFinalizada,
                EventTypes.OrdemServicoFinalizada,
                new OrdemServicoFinalizadaData { OrdemServicoId = 1 },
                version: 0));
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
