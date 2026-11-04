namespace BuildingBlocks.Events;

public sealed class PagamentoConfirmadoData
{
    public required int OrdemServicoId { get; init; }

    public required string CobrancaId { get; init; }
}
