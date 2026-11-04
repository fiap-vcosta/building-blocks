using BuildingBlocks.Events;
using BuildingBlocks.Messaging;
using BuildingBlocks.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace UnitTests.Messaging;

public class MessagingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBuildingBlocksMessaging_RegistersPublisherTransportAndPropagator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBuildingBlocksMessaging(options => options.ProjectId = "demo-project");
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.IsType<GooglePubSubTransport>(provider.GetRequiredService<IPubSubTransport>());
        Assert.IsType<EventPublisher>(provider.GetRequiredService<IEventPublisher>());
        Assert.IsType<DatadogMessageTracePropagator>(provider.GetRequiredService<IMessageTracePropagator>());
        Assert.Equal("demo-project", provider.GetRequiredService<IOptions<PubSubOptions>>().Value.ProjectId);
    }

    [Fact]
    public void AddBuildingBlocksMessaging_WhenServicesNull_Throws()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            MessagingServiceCollectionExtensions.AddBuildingBlocksMessaging(null!));
    }

    [Fact]
    public void AddEventSubscriptionWorker_WhenSubscriptionIdBlank_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act / Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddEventSubscriptionWorker<OrdemServicoFinalizadaData, StubConsumer>(" "));
    }

    [Fact]
    public void AddBuildingBlocksMessaging_WithoutConfigure_RegistersDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBuildingBlocksMessaging();
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.Equal(string.Empty, provider.GetRequiredService<IOptions<PubSubOptions>>().Value.ProjectId);
        Assert.Equal("PubSub", PubSubOptions.SectionName);
    }

    [Fact]
    public void AddEventSubscriptionWorker_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPubSubTransport, InMemoryPubSubTransport>();
        services.AddSingleton<IMessageTracePropagator, RecordingTracePropagator>();

        // Act
        services.AddEventSubscriptionWorker<OrdemServicoFinalizadaData, StubConsumer>("sub-os-finalizada");
        using var provider = services.BuildServiceProvider();
        var hosted = provider.GetServices<IHostedService>().ToList();

        // Assert
        Assert.Contains(hosted, service => service is EventSubscriptionWorker<OrdemServicoFinalizadaData>);
        Assert.IsType<StubConsumer>(provider.GetRequiredService<IIntegrationEventConsumer<OrdemServicoFinalizadaData>>());
    }

    private sealed class StubConsumer : IIntegrationEventConsumer<OrdemServicoFinalizadaData>
    {
        public string EventType => EventTypes.OrdemServicoFinalizada;

        public Task HandleAsync(
            EventEnvelope<OrdemServicoFinalizadaData> envelope,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
