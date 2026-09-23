using System;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Domain.Exceptions;
using Moq;
using Xunit;

namespace ERPFinanceiro.Tests.Application
{
    /// <summary>
    /// Testes de <see cref="ConsultaService.ObterDetalhe"/>/<see cref="ConsultaService.ObterStatus"/>
    /// (T-24), com <see cref="IVendaRepository"/> mockado (Moq) — a lógica sob teste
    /// (cálculo de subtotal, decisão nulo-vs-exceção, itens vazios) não depende de
    /// banco real; a leitura contra `.fdb` já é coberta por
    /// <c>VendaRepositoryTests</c> (T-14). Massa espelha o seed de T-06 (V-1001/V-1003)
    /// para bater com o critério de aceite ("confira contra o seed de T-06").
    /// </summary>
    public class ConsultaServiceTests
    {
        private static readonly DateTime RecebimentoUtc = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime QuitacaoUtc = new DateTime(2026, 9, 3, 14, 30, 0, DateTimeKind.Utc);

        [Fact]
        public void ObterDetalhe_VendaComItens_RetornaCabecalhoEItensComSubtotalCorreto()
        {
            // Espelha V-1001 do seed (T-06): PROD-001 (2 x 150.00 = 300.00) +
            // PROD-002 (1 x 200.50 = 200.50); ValorTotal = 500.50.
            var itens = new[]
            {
                new VendaItem("PROD-001", 2, 150.0000m),
                new VendaItem("PROD-002", 1, 200.5000m)
            };
            var venda = Venda.CriarPorQuitacao(
                vendaId: "V-1001",
                clienteId: "CLI-001",
                valorTotal: 500.50m,
                itens: itens,
                dataRecebimentoUtc: RecebimentoUtc,
                dataQuitacaoUtc: QuitacaoUtc);

            var repositorio = new Mock<IVendaRepository>();
            repositorio.Setup(r => r.ObterPorVendaId("V-1001")).Returns(venda);

            var servico = new ConsultaService(repositorio.Object);
            var detalhe = servico.ObterDetalhe("V-1001");

            Assert.Equal("V-1001", detalhe.VendaId);
            Assert.Equal("CLI-001", detalhe.ClienteId);
            Assert.Equal(500.50m, detalhe.ValorTotal);
            Assert.Equal(StatusVenda.Quitada, detalhe.Status);
            Assert.Equal(2, detalhe.Itens.Count);

            var item1 = Assert.Single(detalhe.Itens, i => i.ProdutoId == "PROD-001");
            Assert.Equal(300.00m, item1.Subtotal);

            var item2 = Assert.Single(detalhe.Itens, i => i.ProdutoId == "PROD-002");
            Assert.Equal(200.50m, item2.Subtotal);

            Assert.Equal(500.50m, detalhe.TotalItens);
        }

        [Fact]
        public void ObterDetalhe_VendaCanceladaSemItens_RetornaListaVaziaSemErro()
        {
            // Espelha V-1003 do seed (T-06): cancelamento de venda desconhecida
            // (D-08/ADR-007) — sem itens, ClienteId/ValorTotal nulos.
            var dataCancelamentoUtc = new DateTime(2026, 9, 7, 16, 45, 0, DateTimeKind.Utc);
            var venda = Venda.CriarCancelada(
                vendaId: "V-1003",
                motivo: "Venda nao localizada no sistema de origem (D-08)",
                dataCancelamentoUtc: dataCancelamentoUtc);

            var repositorio = new Mock<IVendaRepository>();
            repositorio.Setup(r => r.ObterPorVendaId("V-1003")).Returns(venda);

            var servico = new ConsultaService(repositorio.Object);
            var detalhe = servico.ObterDetalhe("V-1003");

            Assert.Equal(StatusVenda.Cancelada, detalhe.Status);
            Assert.Null(detalhe.ClienteId);
            Assert.Null(detalhe.ValorTotal);
            Assert.Empty(detalhe.Itens);
            Assert.Equal(0m, detalhe.TotalItens);
        }

        [Fact]
        public void ObterDetalhe_VendaInexistente_LancaVendaNaoEncontrada()
        {
            var repositorio = new Mock<IVendaRepository>();
            repositorio.Setup(r => r.ObterPorVendaId("V-NAO-EXISTE")).Returns((Venda)null);

            var servico = new ConsultaService(repositorio.Object);

            var excecao = Assert.Throws<VendaNaoEncontradaException>(() => servico.ObterDetalhe("V-NAO-EXISTE"));
            Assert.Equal("V-NAO-EXISTE", excecao.VendaId);
        }

        [Fact]
        public void ObterStatus_VendaExistente_RetornaVendaIdEStatus()
        {
            var venda = Venda.CriarPendente(
                vendaId: "V-1002",
                clienteId: "CLI-002",
                valorTotal: 450.50m,
                itens: new[] { new VendaItem("PROD-003", 3, 100.0000m) },
                dataRecebimentoUtc: RecebimentoUtc);

            var repositorio = new Mock<IVendaRepository>();
            repositorio.Setup(r => r.ObterPorVendaId("V-1002")).Returns(venda);

            var servico = new ConsultaService(repositorio.Object);
            var status = servico.ObterStatus("V-1002");

            Assert.Equal("V-1002", status.VendaId);
            Assert.Equal(StatusVenda.Pendente, status.Status);
        }

        [Fact]
        public void ObterStatus_VendaInexistente_LancaVendaNaoEncontrada()
        {
            var repositorio = new Mock<IVendaRepository>();
            repositorio.Setup(r => r.ObterPorVendaId("V-NAO-EXISTE")).Returns((Venda)null);

            var servico = new ConsultaService(repositorio.Object);

            var excecao = Assert.Throws<VendaNaoEncontradaException>(() => servico.ObterStatus("V-NAO-EXISTE"));
            Assert.Equal("V-NAO-EXISTE", excecao.VendaId);
        }
    }
}
