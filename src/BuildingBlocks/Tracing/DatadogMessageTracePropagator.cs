using System.Diagnostics.CodeAnalysis;
using Datadog.Trace;

namespace BuildingBlocks.Tracing;

[ExcludeFromCodeCoverage]
public sealed class DatadogMessageTracePropagator : IMessageTracePropagator
{
    public void Inject(IDictionary<string, string> attributes, string topicId)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicId);

        using var scope = Tracer.Instance.StartActive($"pubsub.publish {topicId}");
        scope.Span.Type = SpanTypes.Queue;
        scope.Span.ResourceName = topicId;
        scope.Span.SetTag("span.kind", "producer");
        scope.Span.SetTag("messaging.system", "gcp_pubsub");
        scope.Span.SetTag("messaging.destination", topicId);
        scope.Span.SetTag("messaging.operation", "publish");

        var injector = new SpanContextInjector();
        injector.Inject(
            attributes,
            static (carrier, key, value) => carrier[key] = value,
            scope.Span.Context);
    }

    public IDisposable StartConsumerScope(
        string operationName,
        IEnumerable<KeyValuePair<string, string>> attributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(attributes);

        var map = attributes as IReadOnlyDictionary<string, string>
            ?? attributes.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);

        var extractor = new SpanContextExtractor();
        var parent = extractor.Extract(
            map,
            static (carrier, key) =>
                carrier.TryGetValue(key, out var value)
                    ? [value]
                    : Array.Empty<string>());

        var scope = Tracer.Instance.StartActive(
            operationName,
            new SpanCreationSettings { Parent = parent });
        scope.Span.Type = SpanTypes.Queue;
        scope.Span.SetTag("span.kind", "consumer");
        scope.Span.SetTag("messaging.system", "gcp_pubsub");
        scope.Span.SetTag("messaging.operation", "receive");
        return scope;
    }
}
