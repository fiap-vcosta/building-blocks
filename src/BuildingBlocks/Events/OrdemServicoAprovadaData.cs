namespace BuildingBlocks.Events;

public sealed class OrdemServicoAprovadaData
{
    public required int OrdemServicoId { get; init; }

    public required DateTimeOffset AprovadaEm { get; init; }

    public required IReadOnlyList<OrdemServicoAprovadaItem> Itens { get; init; }
}

public sealed class OrdemServicoAprovadaItem
{
    public required int ItemEstoqueId { get; init; }

    public required decimal Quantidade { get; init; }
}
