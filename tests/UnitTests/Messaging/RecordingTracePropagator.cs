using BuildingBlocks.Tracing;

namespace UnitTests.Messaging;

internal sealed class RecordingTracePropagator : IMessageTracePropagator
{
    public int InjectCalls { get; private set; }

    public int ConsumerScopeCalls { get; private set; }

    public void Inject(IDictionary<string, string> attributes)
    {
        InjectCalls++;
        attributes["x-datadog-trace-id"] = "1";
        attributes["x-datadog-parent-id"] = "2";
    }

    public IDisposable? StartConsumerScope(
        string operationName,
        IEnumerable<KeyValuePair<string, string>> attributes)
    {
        ConsumerScopeCalls++;
        return null;
    }
}
