namespace BuildingBlocks.Events;

public sealed class EventEnvelope<TData>
{
    public required Guid EventId { get; init; }

    public required string Type { get; init; }

    public required int Version { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required TData Data { get; init; }
}
