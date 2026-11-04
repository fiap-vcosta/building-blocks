namespace BuildingBlocks.Tracing;

public interface IMessageTracePropagator
{
    void Inject(IDictionary<string, string> attributes);

    IDisposable StartConsumerScope(
        string operationName,
        IEnumerable<KeyValuePair<string, string>> attributes);
}
