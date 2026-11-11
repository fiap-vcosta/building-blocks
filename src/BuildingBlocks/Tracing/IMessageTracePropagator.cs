namespace BuildingBlocks.Tracing;

public interface IMessageTracePropagator
{
    void Inject(IDictionary<string, string> attributes, string topicId);

    IDisposable StartConsumerScope(
        string operationName,
        IEnumerable<KeyValuePair<string, string>> attributes);
}
