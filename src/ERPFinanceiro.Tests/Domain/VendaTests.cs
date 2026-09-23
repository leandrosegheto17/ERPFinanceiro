using System;
using System.Collections.Generic;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Domain.Exceptions;
using Xunit;

namespace ERPFinanceiro.Tests.Domain
{
    /// <summary>
    /// Testes unitários das fábricas Venda.CriarPendente/CriarPorQuitacao (T-07).
    /// Cobre o critério de aceite: enums espelham 0/1/2 do SDD 5; ClienteId/
    /// ValorTotal anuláveis (ADR-007); histórico gerado corretamente em cada fábrica.
    /// </summary>
    public class VendaTests
    {
        private static List<VendaItem> UmItem()
        {
            return new List<VendaItem> { new VendaItem("PROD-1", 2, 10.50m) };
        }

        [Fact]
        public void CriarPendente_DeveNascerPendenteComHistoricoUnicoDeRecebida()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);

            var venda = Venda.CriarPendente(
                vendaId: "V-001",
                clienteId: "CLI-1",
                valorTotal: 21.00m,
                itens: UmItem(),
                dataRecebimentoUtc: dataRecebimento,
                correlationId: "corr-1");

            Assert.Equal("V-001", venda.VendaId);
            Assert.Equal("CLI-1", venda.ClienteId);
            Assert.Equal(21.00m, venda.ValorTotal);
            Assert.Equal(StatusVenda.Pendente, venda.Status);
            Assert.Equal(dataRecebimento, venda.DataRecebimento);
            Assert.Null(venda.DataQuitacao);
            Assert.Null(venda.DataCancelamento);
            Assert.Equal(0, venda.Versao);
            Assert.Single(venda.Itens);

            var registro = Assert.Single(venda.Historico);
            Assert.Equal(Operacao.Recebida, registro.Operacao);
            Assert.Null(registro.StatusAnterior);
            Assert.Equal(StatusVenda.Pendente, registro.StatusNovo);
            Assert.Equal(dataRecebimento, registro.DataHora);
            Assert.Equal("corr-1", registro.CorrelationId);
        }

        [Fact]
        public void CriarPendente_SemClienteId_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Venda.CriarPendente(
                vendaId: "V-002",
                clienteId: null,
                valorTotal: 10m,
                itens: UmItem(),
                dataRecebimentoUtc: DateTime.UtcNow));
        }

        [Fact]
        public void CriarPorQuitacao_DeveNascerQuitadaComHistoricoDeRecebidaEQuitacao()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);
            var dataQuitacao = new DateTime(2026, 9, 22, 10, 30, 0, DateTimeKind.Utc);

            var venda = Venda.CriarPorQuitacao(
                vendaId: "V-100",
                clienteId: "CLI-9",
                valorTotal: 99.90m,
                itens: UmItem(),
                dataRecebimentoUtc: dataRecebimento,
                dataQuitacaoUtc: dataQuitacao,
                correlationId: "corr-2");

            Assert.Equal(StatusVenda.Quitada, venda.Status);
            Assert.Equal("CLI-9", venda.ClienteId);
            Assert.Equal(99.90m, venda.ValorTotal);
            Assert.Equal(dataQuitacao, venda.DataQuitacao);
            Assert.Equal(2, venda.Historico.Count);

            Assert.Equal(Operacao.Recebida, venda.Historico[0].Operacao);
            Assert.Null(venda.Historico[0].StatusAnterior);
            Assert.Equal(StatusVenda.Pendente, venda.Historico[0].StatusNovo);

            Assert.Equal(Operacao.Quitacao, venda.Historico[1].Operacao);
            Assert.Equal(StatusVenda.Pendente, venda.Historico[1].StatusAnterior);
            Assert.Equal(StatusVenda.Quitada, venda.Historico[1].StatusNovo);
            Assert.Equal(dataQuitacao, venda.Historico[1].DataHora);
        }

        [Fact]
        public void CriarPorQuitacao_DataQuitacaoAnteriorARecebimento_DeveLancarArgumentOutOfRangeException()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var dataQuitacao = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);

            Assert.Throws<ArgumentOutOfRangeException>(() => Venda.CriarPorQuitacao(
                vendaId: "V-101",
                clienteId: "CLI-9",
                valorTotal: 10m,
                itens: UmItem(),
                dataRecebimentoUtc: dataRecebimento,
                dataQuitacaoUtc: dataQuitacao));
        }

        [Fact]
        public void VendaItem_QuantidadeZeroOuNegativa_DeveLancarArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VendaItem("PROD-1", 0, 10m));
        }

        [Fact]
        public void VendaItem_PrecoUnitarioNegativo_DeveLancarArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VendaItem("PROD-1", 1, -0.01m));
        }

        private static Venda VendaPendente(DateTime dataRecebimento)
        {
            return Venda.CriarPendente(
                vendaId: "V-200",
                clienteId: "CLI-1",
                valorTotal: 21.00m,
                itens: UmItem(),
                dataRecebimentoUtc: dataRecebimento,
                correlationId: "corr-pend");
        }

        [Fact]
        public void Quitar_VendaPendente_DeveTransicionarParaQuitadaEGerarHistorico()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
            var dataQuitacao = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);
            var venda = VendaPendente(dataRecebimento);

            var resultado = venda.Quitar(dataQuitacao, correlationId: "corr-quit");

            Assert.True(resultado);
            Assert.Equal(StatusVenda.Quitada, venda.Status);
            Assert.Equal(dataQuitacao, venda.DataQuitacao);
            Assert.Equal(2, venda.Historico.Count);

            var registro = venda.Historico[1];
            Assert.Equal(Operacao.Quitacao, registro.Operacao);
            Assert.Equal(StatusVenda.Pendente, registro.StatusAnterior);
            Assert.Equal(StatusVenda.Quitada, registro.StatusNovo);
            Assert.Equal(dataQuitacao, registro.DataHora);
            Assert.Equal("corr-quit", registro.CorrelationId);
        }

        [Fact]
        public void Cancelar_VendaPendente_DeveTransicionarParaCanceladaEGerarHistorico()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
            var dataCancelamento = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);
            var venda = VendaPendente(dataRecebimento);

            var resultado = venda.Cancelar(dataCancelamento, correlationId: "corr-canc");

            Assert.True(resultado);
            Assert.Equal(StatusVenda.Cancelada, venda.Status);
            Assert.Equal(dataCancelamento, venda.DataCancelamento);
            Assert.Equal(2, venda.Historico.Count);

            var registro = venda.Historico[1];
            Assert.Equal(Operacao.Cancelamento, registro.Operacao);
            Assert.Equal(StatusVenda.Pendente, registro.StatusAnterior);
            Assert.Equal(StatusVenda.Cancelada, registro.StatusNovo);
            Assert.Equal(dataCancelamento, registro.DataHora);
            Assert.Equal("corr-canc", registro.CorrelationId);
        }

        [Fact]
        public void Cancelar_VendaQuitada_ComMotivo_DeveTransicionarParaCanceladaEGuardarMotivo()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
            var dataQuitacao = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);
            var dataCancelamento = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var venda = VendaPendente(dataRecebimento);
            venda.Quitar(dataQuitacao);

            var resultado = venda.Cancelar(dataCancelamento, motivo: "Cliente desistiu da compra");

            Assert.True(resultado);
            Assert.Equal(StatusVenda.Cancelada, venda.Status);
            Assert.Equal("Cliente desistiu da compra", venda.MotivoCancelamento);
            Assert.Equal(3, venda.Historico.Count);

            var registro = venda.Historico[2];
            Assert.Equal(Operacao.Cancelamento, registro.Operacao);
            Assert.Equal(StatusVenda.Quitada, registro.StatusAnterior);
            Assert.Equal(StatusVenda.Cancelada, registro.StatusNovo);
            Assert.Equal("Cliente desistiu da compra", registro.Motivo);
        }

        [Fact]
        public void Cancelar_VendaQuitada_SemMotivo_DeveLancarMotivoObrigatorioExceptionSemAlterarEstado()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
            var dataQuitacao = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);
            var dataCancelamento = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var venda = VendaPendente(dataRecebimento);
            venda.Quitar(dataQuitacao);
            var historicoAntes = venda.Historico.Count;
            var versaoAntes = venda.Versao;

            Assert.Throws<MotivoObrigatorioException>(() => venda.Cancelar(dataCancelamento));

            Assert.Equal(StatusVenda.Quitada, venda.Status);
            Assert.Null(venda.DataCancelamento);
            Assert.Null(venda.MotivoCancelamento);
            Assert.Equal(versaoAntes, venda.Versao);
            Assert.Equal(historicoAntes, venda.Historico.Count);
        }

        [Fact]
        public void Cancelar_VendaQuitada_ComMotivoEmBranco_DeveLancarMotivoObrigatorioException()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
            var dataQuitacao = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);
            var dataCancelamento = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var venda = VendaPendente(dataRecebimento);
            venda.Quitar(dataQuitacao);

            Assert.Throws<MotivoObrigatorioException>(() => venda.Cancelar(dataCancelamento, motivo: "   "));

            Assert.Equal(StatusVenda.Quitada, venda.Status);
        }

        [Fact]
        public void Cancelar_VendaPendente_SemMotivo_DeveCancelarNormalmente()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
            var dataCancelamento = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);
            var venda = VendaPendente(dataRecebimento);

            var resultado = venda.Cancelar(dataCancelamento);

            Assert.True(resultado);
            Assert.Equal(StatusVenda.Cancelada, venda.Status);
            Assert.Null(venda.MotivoCancelamento);
        }

        [Fact]
        public void Quitar_VendaCancelada_DeveLancarVendaJaCanceladaException()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
            var dataCancelamento = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);
            var venda = VendaPendente(dataRecebimento);
            venda.Cancelar(dataCancelamento);

            var excecao = Assert.Throws<VendaJaCanceladaException>(() => venda.Quitar(dataCancelamento.AddHours(1)));

            Assert.Equal(venda.VendaId, excecao.VendaId);
        }

        [Fact]
        public void Quitar_VendaJaQuitada_DeveSerIdempotenteSemDuplicarHistorico()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
            var dataQuitacao = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);
            var venda = VendaPendente(dataRecebimento);
            venda.Quitar(dataQuitacao);
            var historicoAntes = venda.Historico.Count;

            var resultado = venda.Quitar(dataQuitacao.AddHours(1));

            Assert.False(resultado);
            Assert.Equal(StatusVenda.Quitada, venda.Status);
            Assert.Equal(historicoAntes, venda.Historico.Count);
        }

        [Fact]
        public void Cancelar_VendaJaCancelada_DeveSerIdempotenteSemDuplicarHistorico()
        {
            var dataRecebimento = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
            var dataCancelamento = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);
            var venda = VendaPendente(dataRecebimento);
            venda.Cancelar(dataCancelamento);
            var historicoAntes = venda.Historico.Count;

            var resultado = venda.Cancelar(dataCancelamento.AddHours(1));

            Assert.False(resultado);
            Assert.Equal(StatusVenda.Cancelada, venda.Status);
            Assert.Equal(historicoAntes, venda.Historico.Count);
        }

        [Fact]
        public void CriarCancelada_VendaDesconhecida_DeveNascerCanceladaSemClienteValorOuItensComHistoricoUnico()
        {
            var dataCancelamento = new DateTime(2026, 9, 22, 11, 0, 0, DateTimeKind.Utc);

            var venda = Venda.CriarCancelada(
                vendaId: "V-1003",
                motivo: "Venda nao localizada no sistema de origem (D-08)",
                dataCancelamentoUtc: dataCancelamento,
                correlationId: "corr-desconhecida");

            Assert.Equal("V-1003", venda.VendaId);
            Assert.Equal(StatusVenda.Cancelada, venda.Status);
            Assert.Null(venda.ClienteId);
            Assert.Null(venda.ValorTotal);
            Assert.Equal(dataCancelamento, venda.DataCancelamento);
            Assert.Equal(dataCancelamento, venda.DataRecebimento);
            Assert.Equal("Venda nao localizada no sistema de origem (D-08)", venda.MotivoCancelamento);
            Assert.Empty(venda.Itens);

            var registro = Assert.Single(venda.Historico);
            Assert.Equal(Operacao.Cancelamento, registro.Operacao);
            Assert.Null(registro.StatusAnterior);
            Assert.Equal(StatusVenda.Cancelada, registro.StatusNovo);
            Assert.Equal(dataCancelamento, registro.DataHora);
            Assert.Equal("corr-desconhecida", registro.CorrelationId);
        }
    }
}
