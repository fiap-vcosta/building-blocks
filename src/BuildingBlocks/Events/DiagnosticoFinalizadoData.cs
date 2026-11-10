namespace BuildingBlocks.Events;

public sealed class DiagnosticoFinalizadoData
{
    public required int OrdemServicoId { get; init; }

    public required int VersaoDiagnostico { get; init; }

    public required DiagnosticoFinalizadoCliente Cliente { get; init; }

    public required IReadOnlyList<DiagnosticoFinalizadoServico> Servicos { get; init; }
}

public sealed class DiagnosticoFinalizadoCliente
{
    public required string Nome { get; init; }

    public required string Email { get; init; }

    public required string Documento { get; init; }
}

public sealed class DiagnosticoFinalizadoServico
{
    public required int Id { get; init; }

    public required string Nome { get; init; }

    public required decimal ValorCobrado { get; init; }

    public required ServicoDiagnosticoStatus Status { get; init; }
}
