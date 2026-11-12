namespace BuildingBlocks.Events;

public sealed class OrcamentoGeradoData
{
    public required Guid OrdemServicoId { get; init; }

    public required int VersaoDiagnostico { get; init; }

    public required string OrcamentoId { get; init; }
}
