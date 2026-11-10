using System.Text.Json;
using BuildingBlocks.Events;

namespace UnitTests.Events;

public class EventJsonTests
{
    [Fact]
    public void Serialize_DiagnosticoFinalizado_MatchesCatalogShape()
    {
        // Arrange
        var envelope = new EventEnvelope<DiagnosticoFinalizadoData>
        {
            EventId = Guid.Parse("3f1c2a0e-7b4d-4e2a-9c11-0a6b8d2e4f10"),
            Type = EventTypes.DiagnosticoFinalizado,
            Version = 2,
            OccurredAt = DateTimeOffset.Parse("2026-10-25T15:04:05Z"),
            Data = new DiagnosticoFinalizadoData
            {
                OrdemServicoId = 10,
                VersaoDiagnostico = 1,
                Cliente = new DiagnosticoFinalizadoCliente
                {
                    Nome = "Ada",
                    Email = "ada@oficina.dev",
                    Documento = "86421537090"
                },
                Servicos =
                [
                    new DiagnosticoFinalizadoServico
                    {
                        Id = 3,
                        Nome = "Troca de óleo",
                        ValorCobrado = 150.0m,
                        Status = ServicoDiagnosticoStatus.Sugerido
                    }
                ]
            }
        };

        // Act
        using var document = JsonDocument.Parse(EventJson.Serialize(envelope));
        var root = document.RootElement;

        // Assert
        Assert.Equal(Guid.Parse("3f1c2a0e-7b4d-4e2a-9c11-0a6b8d2e4f10"), root.GetProperty("eventId").GetGuid());
        Assert.Equal(EventTypes.DiagnosticoFinalizado, root.GetProperty("type").GetString());
        Assert.Equal(2, root.GetProperty("version").GetInt32());
        Assert.Equal(
            "2026-10-25T15:04:05Z",
            root.GetProperty("occurredAt").GetDateTimeOffset().UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        var data = root.GetProperty("data");
        Assert.Equal(10, data.GetProperty("ordemServicoId").GetInt32());
        Assert.Equal(1, data.GetProperty("versaoDiagnostico").GetInt32());
        var cliente = data.GetProperty("cliente");
        Assert.Equal("Ada", cliente.GetProperty("nome").GetString());
        Assert.Equal("ada@oficina.dev", cliente.GetProperty("email").GetString());
        Assert.Equal("86421537090", cliente.GetProperty("documento").GetString());
        var servico = data.GetProperty("servicos")[0];
        Assert.Equal(3, servico.GetProperty("id").GetInt32());
        Assert.Equal("Troca de óleo", servico.GetProperty("nome").GetString());
        Assert.Equal(150.0m, servico.GetProperty("valorCobrado").GetDecimal());
        Assert.Equal("Sugerido", servico.GetProperty("status").GetString());
    }

    [Fact]
    public void RoundTrip_OrcamentoGerado_PreservesValues()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.OrcamentoGerado,
            new OrcamentoGeradoData
            {
                OrdemServicoId = 10,
                VersaoDiagnostico = 1,
                OrcamentoId = "orc-10-1"
            });

        // Act
        var result = EventJson.Deserialize<OrcamentoGeradoData>(EventJson.Serialize(envelope));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(envelope.EventId, result.EventId);
        Assert.Equal("orc-10-1", result.Data.OrcamentoId);
    }

    [Fact]
    public void RoundTrip_OrcamentoFalhou_PreservesMotivo()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.OrcamentoFalhou,
            new OrcamentoFalhouData
            {
                OrdemServicoId = 10,
                VersaoDiagnostico = 1,
                Motivo = "falha ao gravar o orçamento"
            });

        // Act
        var result = EventJson.Deserialize<OrcamentoFalhouData>(EventJson.Serialize(envelope));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("falha ao gravar o orçamento", result.Data.Motivo);
    }

    [Fact]
    public void Serialize_OrdemServicoAprovada_UsesIso8601AprovadaEm()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.OrdemServicoAprovada,
            new OrdemServicoAprovadaData
            {
                OrdemServicoId = 10,
                AprovadaEm = DateTimeOffset.Parse("2026-10-25T15:10:00Z"),
                Itens = [new OrdemServicoAprovadaItem { ItemEstoqueId = 7, Quantidade = 2 }]
            });

        // Act
        using var document = JsonDocument.Parse(EventJson.Serialize(envelope));
        var data = document.RootElement.GetProperty("data");

        // Assert
        Assert.Equal(
            "2026-10-25T15:10:00Z",
            data.GetProperty("aprovadaEm").GetDateTimeOffset().UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        Assert.Equal(7, data.GetProperty("itens")[0].GetProperty("itemEstoqueId").GetInt32());
        Assert.Equal(2m, data.GetProperty("itens")[0].GetProperty("quantidade").GetDecimal());
    }

    [Fact]
    public void Serialize_EstoqueReservado_UsesEstoqueTravadoStatusName()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.EstoqueReservado,
            new EstoqueReservadoData
            {
                OrdemServicoId = 10,
                AprovadaEm = DateTimeOffset.Parse("2026-10-25T15:10:00Z"),
                Itens =
                [
                    new EstoqueReservadoItem
                    {
                        ItemEstoqueId = 7,
                        Quantidade = 2,
                        Status = ItemEstoqueEventoStatus.EstoqueTravado
                    }
                ]
            });

        // Act
        using var document = JsonDocument.Parse(EventJson.Serialize(envelope));

        // Assert
        Assert.Equal(
            "EstoqueTravado",
            document.RootElement.GetProperty("data").GetProperty("itens")[0].GetProperty("status").GetString());
    }

    [Fact]
    public void Serialize_EstoqueEmFalta_UsesCatalogStatusNames()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.EstoqueEmFalta,
            new EstoqueEmFaltaData
            {
                OrdemServicoId = 10,
                AprovadaEm = DateTimeOffset.Parse("2026-10-25T15:10:00Z"),
                Itens =
                [
                    new EstoqueEmFaltaItem
                    {
                        ItemEstoqueId = 7,
                        Quantidade = 2,
                        Status = ItemEstoqueEventoStatus.EstoqueEmFalta
                    }
                ]
            });

        // Act
        using var document = JsonDocument.Parse(EventJson.Serialize(envelope));

        // Assert
        Assert.Equal(
            "EstoqueEmFalta",
            document.RootElement.GetProperty("data").GetProperty("itens")[0].GetProperty("status").GetString());
    }

    [Fact]
    public void RoundTrip_OrdemServicoFinalizada_PreservesId()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.OrdemServicoFinalizada,
            new OrdemServicoFinalizadaData { OrdemServicoId = 10 });

        // Act
        var result = EventJson.Deserialize<OrdemServicoFinalizadaData>(EventJson.Serialize(envelope));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Data.OrdemServicoId);
    }

    [Fact]
    public void RoundTrip_CobrancaGerada_PreservesCobrancaId()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.CobrancaGerada,
            new CobrancaGeradaData { OrdemServicoId = 10, CobrancaId = "ord_mp_1" });

        // Act
        var result = EventJson.Deserialize<CobrancaGeradaData>(EventJson.Serialize(envelope));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ord_mp_1", result.Data.CobrancaId);
    }

    [Fact]
    public void RoundTrip_PagamentoConfirmado_PreservesIds()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.PagamentoConfirmado,
            new PagamentoConfirmadoData { OrdemServicoId = 10, CobrancaId = "ord_mp_1" });

        // Act
        var result = EventJson.Deserialize<PagamentoConfirmadoData>(EventJson.Serialize(envelope));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Data.OrdemServicoId);
        Assert.Equal("ord_mp_1", result.Data.CobrancaId);
    }

    [Fact]
    public void Serialize_PagamentoRecusado_UsesLowercaseMotivo()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.PagamentoRecusado,
            new PagamentoRecusadoData
            {
                OrdemServicoId = 10,
                CobrancaId = "ord_mp_1",
                Motivo = PagamentoRecusadoMotivos.Recusado
            });

        // Act
        using var document = JsonDocument.Parse(EventJson.Serialize(envelope));

        // Assert
        Assert.Equal(
            "recusado",
            document.RootElement.GetProperty("data").GetProperty("motivo").GetString());
    }

    [Fact]
    public void RoundTrip_OrdemServicoDescartada_PreservesId()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.OrdemServicoDescartada,
            new OrdemServicoDescartadaData { OrdemServicoId = 10 });

        // Act
        var result = EventJson.Deserialize<OrdemServicoDescartadaData>(EventJson.Serialize(envelope));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Data.OrdemServicoId);
    }

    [Fact]
    public void Serialize_OrcamentoDisponivel_MatchesCatalogShape()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.OrcamentoDisponivel,
            new OrcamentoDisponivelData
            {
                OrdemServicoId = 10,
                VersaoDiagnostico = 1,
                OrcamentoId = "orc-10-1",
                ValorTotal = 150.0m,
                Servicos =
                [
                    new OrcamentoDisponivelServico { Nome = "Troca de óleo", ValorCobrado = 150.0m }
                ]
            });

        // Act
        using var document = JsonDocument.Parse(EventJson.Serialize(envelope));
        var data = document.RootElement.GetProperty("data");

        // Assert
        Assert.Equal(150.0m, data.GetProperty("valorTotal").GetDecimal());
        Assert.Equal("Troca de óleo", data.GetProperty("servicos")[0].GetProperty("nome").GetString());
        Assert.Equal(EventTypes.OrcamentoDisponivel, document.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public void Serialize_CobrancaPixDisponivel_MatchesCatalogShape()
    {
        // Arrange
        var envelope = CreateEnvelope(
            EventTypes.CobrancaPixDisponivel,
            new CobrancaPixDisponivelData
            {
                OrdemServicoId = 10,
                CobrancaId = "ord_mp_1",
                ValorTotal = 150.0m,
                QrCode = "00020126...",
                Ticket = "https://www.mercadopago.com/sandbox/ticket/1"
            });

        // Act
        using var document = JsonDocument.Parse(EventJson.Serialize(envelope));
        var data = document.RootElement.GetProperty("data");

        // Assert
        Assert.Equal("00020126...", data.GetProperty("qrCode").GetString());
        Assert.Equal("https://www.mercadopago.com/sandbox/ticket/1", data.GetProperty("ticket").GetString());
        Assert.Equal(EventTypes.CobrancaPixDisponivel, document.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public void TopicIds_MatchInfraEventsCatalog()
    {
        // Arrange / Act / Assert
        Assert.Equal("diagnostico-finalizado", TopicIds.DiagnosticoFinalizado);
        Assert.Equal("orcamento-gerado", TopicIds.OrcamentoGerado);
        Assert.Equal("orcamento-falhou", TopicIds.OrcamentoFalhou);
        Assert.Equal("ordem-servico-aprovada", TopicIds.OrdemServicoAprovada);
        Assert.Equal("estoque-reservado", TopicIds.EstoqueReservado);
        Assert.Equal("estoque-em-falta", TopicIds.EstoqueEmFalta);
        Assert.Equal("ordem-servico-finalizada", TopicIds.OrdemServicoFinalizada);
        Assert.Equal("cobranca-gerada", TopicIds.CobrancaGerada);
        Assert.Equal("pagamento-confirmado", TopicIds.PagamentoConfirmado);
        Assert.Equal("pagamento-recusado", TopicIds.PagamentoRecusado);
        Assert.Equal("ordem-servico-descartada", TopicIds.OrdemServicoDescartada);
        Assert.Equal("notificacoes", TopicIds.Notificacoes);
    }

    private static EventEnvelope<T> CreateEnvelope<T>(string type, T data) =>
        new()
        {
            EventId = Guid.NewGuid(),
            Type = type,
            Version = 1,
            OccurredAt = DateTimeOffset.UtcNow,
            Data = data
        };
}
