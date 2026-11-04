namespace BuildingBlocks.Events;

public sealed class OrcamentoGeradoData
{
    public required int OrdemServicoId { get; init; }

    public required int VersaoDiagnostico { get; init; }

    public required string OrcamentoId { get; init; }
}
