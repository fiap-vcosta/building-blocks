namespace BuildingBlocks.Events;

public sealed class CobrancaPixDisponivelData
{
    public required int OrdemServicoId { get; init; }

    public required string CobrancaId { get; init; }

    public required decimal ValorTotal { get; init; }

    public required string QrCode { get; init; }

    public required string Ticket { get; init; }
}
