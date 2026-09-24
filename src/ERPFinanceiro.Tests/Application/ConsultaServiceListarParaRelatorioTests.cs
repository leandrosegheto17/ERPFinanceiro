using System;
using System.Collections.Generic;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;
using Moq;
using Xunit;

namespace ERPFinanceiro.Tests.Application
{
    /// <summary>
    /// Testes de <see cref="ConsultaService.ListarParaRelatorio"/> (T-48), com
    /// <see cref="IVendaRepository"/>/<see cref="IVendaConsultaLeitura"/> mockados (Moq) —
    /// cobre a lógica de agregação (Total listado, contagem de nulos) sem depender de
    /// banco real; a integração real contra Firebird embarcado com massa equivalente ao
    /// seed de T-06 está em
    /// <c>ERPFinanceiro.Tests.Reports.RelatorioDataSourceTests</c>.
    /// </summary>
    public class ConsultaServiceListarParaRelatorioTests
    {
        private static readonly DateTime DataBase = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void ListarParaRelatorio_ComMassaEquivalenteAoSeedT06_TotalListadoBateComCA073()
        {
            // Espelha database/02-seed.sql (T-06, CA-07.3): 500.50 + 450.50 + null(=0) = 951.00.
            var v1001 = Venda.CriarPorQuitacao(
                vendaId: "V-1001",
                clienteId: "CLI-001",
                valorTotal: 500.50m,
                itens: new[] { new VendaItem("PROD-001", 2, 150.0000m) },
                dataRecebimentoUtc: DataBase,
                dataQuitacaoUtc: DataBase.AddDays(2));

            var v1002 = Venda.CriarPendente(
                vendaId: "V-1002",
                clienteId: "CLI-002",
                valorTotal: 450.50m,
                itens: new[] { new VendaItem("PROD-003", 3, 100.0000m) },
                dataRecebimentoUtc: DataBase.AddDays(4));

            var v1003 = Venda.CriarCancelada(
                vendaId: "V-1003",
                motivo: "Venda desconhecida cancelada pelo cliente",
                dataCancelamentoUtc: DataBase.AddDays(6));

            var leitura = new Mock<IVendaConsultaLeitura>();
            leitura.Setup(l => l.ListarTodas())
                .Returns(new List<Venda> { v1003, v1002, v1001 }); // já ordenado DataRecebimento desc

            var servico = new ConsultaService(new Mock<IVendaRepository>().Object, leitura.Object);

            var resultado = servico.ListarParaRelatorio(new FiltroVendas());

            Assert.Equal(3, resultado.Linhas.Count);
            Assert.Equal(951.00m, resultado.TotalListado);
            Assert.Equal(1, resultado.QuantidadeSemValor); // só V-1003 tem ValorTotal nulo

            var linhaV1003 = resultado.Linhas[0];
            Assert.Equal("V-1003", linhaV1003.VendaId);
            Assert.Null(linhaV1003.ValorTotal);
            Assert.Null(linhaV1003.ClienteId);
        }

        [Fact]
        public void ListarParaRelatorio_ListaVazia_TotalListadoZeroENenhumaLinha()
        {
            var leitura = new Mock<IVendaConsultaLeitura>();
            leitura.Setup(l => l.ListarTodas()).Returns(new List<Venda>());

            var servico = new ConsultaService(new Mock<IVendaRepository>().Object, leitura.Object);

            var resultado = servico.ListarParaRelatorio(new FiltroVendas());

            Assert.Empty(resultado.Linhas);
            Assert.Equal(0.00m, resultado.TotalListado);
            Assert.Equal(0, resultado.QuantidadeSemValor);
        }

        [Fact]
        public void ListarParaRelatorio_SemIVendaConsultaLeitura_LancaInvalidOperationException()
        {
            var servico = new ConsultaService(new Mock<IVendaRepository>().Object);

            Assert.Throws<InvalidOperationException>(() => servico.ListarParaRelatorio(new FiltroVendas()));
        }
    }
}
