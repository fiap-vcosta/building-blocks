namespace BuildingBlocks.Events;

public sealed class PagamentoConfirmadoData
{
    public required Guid OrdemServicoId { get; init; }

    public required string CobrancaId { get; init; }
}
