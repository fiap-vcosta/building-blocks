using BuildingBlocks.Events;
using BuildingBlocks.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Messaging;

public class EventSubscriptionWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_AcksWhenHandlerSucceeds()
    {
        // Arrange
        const string subscriptionId = "sub-finalizada";
        var transport = new InMemoryPubSubTransport();
        transport.BindSubscription(TopicIds.OrdemServicoFinalizada, subscriptionId);

        var handler = new RecordingHandler();
        var propagator = new RecordingTracePropagator();
        var worker = new EventSubscriptionWorker<OrdemServicoFinalizadaData>(
            transport,
            handler,
            propagator,
            subscriptionId,
            NullLogger<EventSubscriptionWorker<OrdemServicoFinalizadaData>>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var run = worker.StartAsync(cts.Token);

        var publisher = new EventPublisher(transport, propagator);

        // Act
        await publisher.PublishAsync(
            TopicIds.OrdemServicoFinalizada,
            EventTypes.OrdemServicoFinalizada,
            new OrdemServicoFinalizadaData { OrdemServicoId = 42 });

        await WaitForAsync(() => handler.Calls.Count > 0, TimeSpan.FromSeconds(2));
        await cts.CancelAsync();
        await run;

        // Assert
        Assert.Single(handler.Calls);
        Assert.Equal(42, handler.Calls[0].Data.OrdemServicoId);
        Assert.True(propagator.ConsumerScopeCalls >= 1);
    }

    [Fact]
    public async Task ExecuteAsync_StopsCleanlyWhenSubscriptionCompletes()
    {
        // Arrange
        const string subscriptionId = "sub-complete-cleanly";
        var transport = new CompletingTrackingTransport();
        transport.BindSubscription(TopicIds.OrdemServicoFinalizada, subscriptionId);

        var handler = new RecordingHandler();
        var worker = new EventSubscriptionWorker<OrdemServicoFinalizadaData>(
            transport,
            handler,
            new RecordingTracePropagator(),
            subscriptionId,
            NullLogger<EventSubscriptionWorker<OrdemServicoFinalizadaData>>.Instance);

        var run = worker.StartAsync(CancellationToken.None);

        // Act
        await new EventPublisher(transport, new RecordingTracePropagator()).PublishAsync(
            TopicIds.OrdemServicoFinalizada,
            EventTypes.OrdemServicoFinalizada,
            new OrdemServicoFinalizadaData { OrdemServicoId = 7 });

        await WaitForAsync(() => handler.Calls.Count > 0, TimeSpan.FromSeconds(2));
        transport.Complete(subscriptionId);
        await run;

        // Assert
        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_NacksWhenBodyIsInvalidJson()
    {
        // Arrange
        const string subscriptionId = "sub-invalid-json";
        var transport = new TrackingTransport();
        transport.BindSubscription(TopicIds.OrdemServicoFinalizada, subscriptionId);
        var logger = new CapturingLogger<EventSubscriptionWorker<OrdemServicoFinalizadaData>>();

        var worker = new EventSubscriptionWorker<OrdemServicoFinalizadaData>(
            transport,
            new RecordingHandler(),
            new RecordingTracePropagator(),
            subscriptionId,
            logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var run = worker.StartAsync(cts.Token);

        // Act
        await transport.PublishAsync(
            TopicIds.OrdemServicoFinalizada,
            "not-json",
            new Dictionary<string, string>(),
            CancellationToken.None);

        await WaitForAsync(() => transport.Nacked.Count > 0, TimeSpan.FromSeconds(2));
        await cts.CancelAsync();
        await run;

        // Assert
        Assert.Single(transport.Nacked);
        Assert.Contains(logger.Entries, entry => entry.LogLevel == LogLevel.Error);
    }

    [Fact]
    public async Task ExecuteAsync_NacksWhenBodyIsJsonNull()
    {
        // Arrange
        const string subscriptionId = "sub-json-null";
        var transport = new TrackingTransport();
        transport.BindSubscription(TopicIds.OrdemServicoFinalizada, subscriptionId);

        var worker = new EventSubscriptionWorker<OrdemServicoFinalizadaData>(
            transport,
            new RecordingHandler(),
            new RecordingTracePropagator(),
            subscriptionId,
            NullLogger<EventSubscriptionWorker<OrdemServicoFinalizadaData>>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var run = worker.StartAsync(cts.Token);

        // Act
        await transport.PublishAsync(
            TopicIds.OrdemServicoFinalizada,
            "null",
            new Dictionary<string, string>(),
            CancellationToken.None);

        await WaitForAsync(() => transport.Nacked.Count > 0, TimeSpan.FromSeconds(2));
        await cts.CancelAsync();
        await run;

        // Assert
        Assert.Single(transport.Nacked);
    }

    [Fact]
    public async Task ExecuteAsync_NacksWhenEventTypeDoesNotMatch()
    {
        // Arrange
        const string subscriptionId = "sub-mismatch";
        var transport = new TrackingTransport();
        transport.BindSubscription(TopicIds.OrcamentoGerado, subscriptionId);

        var handler = new RecordingOrcamentoHandler();
        var worker = new EventSubscriptionWorker<OrcamentoGeradoData>(
            transport,
            handler,
            new RecordingTracePropagator(),
            subscriptionId,
            NullLogger<EventSubscriptionWorker<OrcamentoGeradoData>>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var run = worker.StartAsync(cts.Token);

        var wrongEnvelope = new EventEnvelope<OrcamentoGeradoData>
        {
            EventId = Guid.NewGuid(),
            Type = EventTypes.OrcamentoFalhou,
            Version = 1,
            OccurredAt = DateTimeOffset.UtcNow,
            Data = new OrcamentoGeradoData
            {
                OrdemServicoId = 1,
                VersaoDiagnostico = 1,
                OrcamentoId = "orc-1"
            }
        };

        // Act
        await transport.PublishAsync(
            TopicIds.OrcamentoGerado,
            EventJson.Serialize(wrongEnvelope),
            new Dictionary<string, string> { ["eventType"] = EventTypes.OrcamentoFalhou },
            CancellationToken.None);

        await WaitForAsync(() => transport.Nacked.Count > 0, TimeSpan.FromSeconds(2));
        await cts.CancelAsync();
        await run;

        // Assert
        Assert.Empty(handler.Calls);
        Assert.Single(transport.Nacked);
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("Condition was not met before timeout.");
    }

    private sealed class RecordingHandler : IIntegrationEventConsumer<OrdemServicoFinalizadaData>
    {
        public string EventType => EventTypes.OrdemServicoFinalizada;

        public List<EventEnvelope<OrdemServicoFinalizadaData>> Calls { get; } = [];

        public Task HandleAsync(
            EventEnvelope<OrdemServicoFinalizadaData> envelope,
            CancellationToken cancellationToken)
        {
            Calls.Add(envelope);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingOrcamentoHandler : IIntegrationEventConsumer<OrcamentoGeradoData>
    {
        public string EventType => EventTypes.OrcamentoGerado;

        public List<EventEnvelope<OrcamentoGeradoData>> Calls { get; } = [];

        public Task HandleAsync(
            EventEnvelope<OrcamentoGeradoData> envelope,
            CancellationToken cancellationToken)
        {
            Calls.Add(envelope);
            return Task.CompletedTask;
        }
    }

    private class TrackingTransport : InMemoryPubSubTransport
    {
        public List<string> Nacked { get; } = [];

        public override Task NegativeAcknowledgeAsync(
            string subscriptionId,
            string messageId,
            CancellationToken cancellationToken)
        {
            Nacked.Add(messageId);
            return base.NegativeAcknowledgeAsync(subscriptionId, messageId, cancellationToken);
        }
    }

    private sealed class CompletingTrackingTransport : TrackingTransport
    {
        public void Complete(string subscriptionId)
        {
            var field = typeof(InMemoryPubSubTransport)
                .GetField(
                    "_subscriptions",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(field);
            var subscriptions = (System.Collections.Concurrent.ConcurrentDictionary<
                string,
                System.Threading.Channels.Channel<PubSubMessage>>)field.GetValue(this)!;
            Assert.True(subscriptions.TryGetValue(subscriptionId, out var channel));
            channel.Writer.TryComplete();
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel LogLevel, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
