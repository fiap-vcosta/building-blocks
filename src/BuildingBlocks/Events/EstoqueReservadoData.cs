namespace BuildingBlocks.Events;

public sealed class EstoqueReservadoData
{
    public required Guid OrdemServicoId { get; init; }

    public required DateTimeOffset AprovadaEm { get; init; }

    public required IReadOnlyList<EstoqueReservadoItem> Itens { get; init; }
}

public sealed class EstoqueReservadoItem
{
    public required int ItemEstoqueId { get; init; }

    public required decimal Quantidade { get; init; }

    public required ItemEstoqueEventoStatus Status { get; init; }
}
