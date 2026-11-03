namespace BuildingBlocks.Events;

public sealed class PagamentoRecusadoData
{
    public required int OrdemServicoId { get; init; }

    public required string CobrancaId { get; init; }

    public required string Motivo { get; init; }
}
