namespace BuildingBlocks.Events;

public sealed class PagamentoRecusadoData
{
    public required Guid OrdemServicoId { get; init; }

    public required string CobrancaId { get; init; }

    public required string Motivo { get; init; }
}
