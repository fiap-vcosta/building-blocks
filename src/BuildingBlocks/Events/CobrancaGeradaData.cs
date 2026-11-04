namespace BuildingBlocks.Events;

public sealed class CobrancaGeradaData
{
    public required int OrdemServicoId { get; init; }

    public required string CobrancaId { get; init; }
}
