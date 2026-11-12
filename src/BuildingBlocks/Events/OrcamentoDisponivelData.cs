namespace BuildingBlocks.Events;

public sealed class OrcamentoDisponivelData
{
    public required Guid OrdemServicoId { get; init; }

    public required int VersaoDiagnostico { get; init; }

    public required string OrcamentoId { get; init; }

    public required decimal ValorTotal { get; init; }

    public required IReadOnlyList<OrcamentoDisponivelServico> Servicos { get; init; }
}

public sealed class OrcamentoDisponivelServico
{
    public required string Nome { get; init; }

    public required decimal ValorCobrado { get; init; }
}
