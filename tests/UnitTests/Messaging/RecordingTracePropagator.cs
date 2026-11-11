using BuildingBlocks.Tracing;

namespace UnitTests.Messaging;

internal sealed class RecordingTracePropagator : IMessageTracePropagator
{
    public int InjectCalls { get; private set; }

    public int ConsumerScopeCalls { get; private set; }

    public string? LastInjectedTopicId { get; private set; }

    public void Inject(IDictionary<string, string> attributes, string topicId)
    {
        InjectCalls++;
        LastInjectedTopicId = topicId;
        attributes["x-datadog-trace-id"] = "1";
        attributes["x-datadog-parent-id"] = "2";
    }

    public IDisposable StartConsumerScope(
        string operationName,
        IEnumerable<KeyValuePair<string, string>> attributes)
    {
        ConsumerScopeCalls++;
        return EmptyScope.Instance;
    }

    private sealed class EmptyScope : IDisposable
    {
        public static EmptyScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
