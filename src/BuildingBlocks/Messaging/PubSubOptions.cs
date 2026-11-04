namespace BuildingBlocks.Messaging;

public sealed class PubSubOptions
{
    public const string SectionName = "PubSub";

    public string ProjectId { get; set; } = string.Empty;
}
