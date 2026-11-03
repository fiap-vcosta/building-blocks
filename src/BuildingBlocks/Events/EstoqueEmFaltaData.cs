namespace BuildingBlocks.Events;

public sealed class EstoqueEmFaltaData
{
    public required int OrdemServicoId { get; init; }

    public required DateTimeOffset AprovadaEm { get; init; }

    public required IReadOnlyList<EstoqueEmFaltaItem> Itens { get; init; }
}

public sealed class EstoqueEmFaltaItem
{
    public required int ItemEstoqueId { get; init; }

    public required decimal Quantidade { get; init; }

    public required ItemEstoqueEventoStatus Status { get; init; }
}
