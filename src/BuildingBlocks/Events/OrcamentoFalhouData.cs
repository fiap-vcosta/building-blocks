namespace BuildingBlocks.Events;

public sealed class OrcamentoFalhouData
{
    public required int OrdemServicoId { get; init; }

    public required int VersaoDiagnostico { get; init; }

    public required string Motivo { get; init; }
}
