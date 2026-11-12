namespace BuildingBlocks.Events;

public sealed class CobrancaGeradaData
{
    public required Guid OrdemServicoId { get; init; }

    public required string CobrancaId { get; init; }
}
