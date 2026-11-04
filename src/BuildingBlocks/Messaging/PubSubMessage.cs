namespace BuildingBlocks.Messaging;

public sealed class PubSubMessage
{
    public required string Id { get; init; }

    public required string Data { get; init; }

    public IDictionary<string, string> Attributes { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
