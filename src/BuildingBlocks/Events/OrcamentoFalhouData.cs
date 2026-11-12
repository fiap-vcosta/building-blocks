namespace BuildingBlocks.Events;

public sealed class OrcamentoFalhouData
{
    public required Guid OrdemServicoId { get; init; }

    public required int VersaoDiagnostico { get; init; }

    public required string Motivo { get; init; }
}
