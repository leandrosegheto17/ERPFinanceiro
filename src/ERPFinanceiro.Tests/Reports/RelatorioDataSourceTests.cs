using System;
using System.Collections.Generic;
using System.Linq;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;
using Moq;
using ERPFinanceiro.Reports;
using Xunit;

namespace ERPFinanceiro.Tests.Reports
{
    public class RelatorioDataSourceTests
    {
        private static Venda V(string id, string cli, decimal? valor, DateTime d)
        {
            if (cli == null) return Venda.CriarCancelada(id, "desconhecida", d);
            return Venda.CriarPendente(id, cli, valor.Value, new[] { new VendaItem("P", 1, valor.Value) }, d);
        }

        private static RelatorioDataSource Criar(IReadOnlyList<Venda> vendas)
        {
            var leitura = new Mock<IVendaConsultaLeitura>();
            leitura.Setup(l => l.ListarMaisRecentes(It.IsAny<int>()))
                   .Returns<int>(n => vendas.Take(n).ToList());
            var repo = new Mock<IVendaRepository>();
            return new RelatorioDataSource(new ConsultaService(repo.Object, leitura.Object));
        }

        [Fact]
        public void MassaSeedT06_TotalListadoE951_ComNuloComoZeroEContagem()
        {
            var r = Criar(new[]
            {
                V("V-1002", "CLI-002", 450.50m, new DateTime(2026, 9, 5)),
                V("V-1003", null, null, new DateTime(2026, 9, 3)),
                V("V-1001", "CLI-001", 500.50m, new DateTime(2026, 9, 1))
            }).Obter(new FiltroVendas());

            Assert.Equal(951.00m, r.TotalListado);
            Assert.Equal(1, r.QuantidadeValoresNulos);
            Assert.Equal(3, r.Linhas.Count);
            Assert.Equal(0m, r.Linhas[1].ValorTotal);
            Assert.Equal("—", r.Linhas[1].ClienteId);
            Assert.False(string.IsNullOrEmpty(r.NotaRodape));
        }

        [Fact]
        public void ListaVazia_TotalZero()
        {
            var r = Criar(new Venda[0]).Obter(new FiltroVendas());
            Assert.Equal(0.00m, r.TotalListado);
            Assert.Empty(r.Linhas);
            Assert.Equal(string.Empty, r.NotaRodape);
        }

        [Fact]
        public void MaisDe5000_NaoTrunca()
        {
            var vendas = Enumerable.Range(0, 5001)
                .Select(i => V("V-" + i, "C", 1m, new DateTime(2026, 1, 1).AddMinutes(-i))).ToList();
            var r = Criar(vendas).Obter(new FiltroVendas());
            Assert.Equal(5001, r.Linhas.Count);
            Assert.Equal(5001m, r.TotalListado);
        }
    }
}
