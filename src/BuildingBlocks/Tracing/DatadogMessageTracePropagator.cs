using System.Diagnostics.CodeAnalysis;
using Datadog.Trace;

namespace BuildingBlocks.Tracing;

[ExcludeFromCodeCoverage]
public sealed class DatadogMessageTracePropagator : IMessageTracePropagator
{
    public void Inject(IDictionary<string, string> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        var context = Tracer.Instance.ActiveScope?.Span?.Context;
        if (context is null)
        {
            return;
        }

        var injector = new SpanContextInjector();
        injector.Inject(
            attributes,
            static (carrier, key, value) => carrier[key] = value,
            context);
    }

    public IDisposable? StartConsumerScope(
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

        return Tracer.Instance.StartActive(
            operationName,
            new SpanCreationSettings { Parent = parent });
    }
}
