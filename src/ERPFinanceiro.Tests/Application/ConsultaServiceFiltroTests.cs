using System;
using System.Collections.Generic;
using System.Linq;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Exceptions;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using Moq;
using Xunit;

namespace ERPFinanceiro.Tests.Application
{
    /// <summary>
    /// T-57: aplicação de <see cref="FiltroVendas"/> (repositório em memória, sem Firebird).
    /// Massa espelha o seed T-06 + vendas extras para combinar filtros.
    /// </summary>
    public class ConsultaServiceFiltroTests
    {
        private static readonly DateTime D0 = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

        private sealed class LeituraMemoria : IVendaConsultaLeitura
        {
            private readonly List<Venda> _vendas;
            public LeituraMemoria(IEnumerable<Venda> v) { _vendas = v.ToList(); }
            public IReadOnlyList<Venda> ListarMaisRecentes(int max) =>
                _vendas.AsQueryable().OrderByDescending(v => v.DataRecebimento).Take(max).ToList();
            public IReadOnlyList<Venda> ListarTodas() =>
                _vendas.AsQueryable().OrderByDescending(v => v.DataRecebimento).ToList();
            public IReadOnlyList<Venda> ListarMaisRecentes(FiltroVendas f, int max) =>
                _vendas.AsQueryable().Aplicar(f).OrderByDescending(v => v.DataRecebimento).Take(max).ToList();
            public IReadOnlyList<Venda> ListarTodas(FiltroVendas f) =>
                _vendas.AsQueryable().Aplicar(f).OrderByDescending(v => v.DataRecebimento).ToList();
        }

        private static Venda Pend(string id, string cli, DateTime rec) =>
            Venda.CriarPendente(id, cli, 100m, new[] { new VendaItem("P", 1, 100m) }, rec);

        private static Venda Quit(string id, string cli, DateTime rec) =>
            Venda.CriarPorQuitacao(id, cli, 100m, new[] { new VendaItem("P", 1, 100m) }, rec, rec.AddDays(1));

        // V1 CLI-001 quitada D0; V2 CLI-002 pendente D0+4; V3 CLI-0021 pendente D0+8; V4 CLI-001 quitada D0+12
        private static ConsultaService Servico(IEnumerable<Venda> extra = null)
        {
            var vendas = new List<Venda>
            {
                Quit("V1", "CLI-001", D0),
                Pend("V2", "CLI-002", D0.AddDays(4)),
                Pend("V3", "CLI-0021", D0.AddDays(8)),
                Quit("V4", "CLI-001", D0.AddDays(12)),
                Venda.CriarCancelada("V5", "motivo", D0.AddDays(20))
            };
            if (extra != null) vendas.AddRange(extra);
            return new ConsultaService(new Mock<IVendaRepository>().Object, new LeituraMemoria(vendas));
        }

        private static string[] Ids(ResultadoListagemVendas r) => r.Itens.Select(i => i.VendaId).ToArray();

        [Fact]
        public void SemFiltro_RetornaTudoOrdenadoDesc()
        {
            Assert.Equal(new[] { "V5", "V4", "V3", "V2", "V1" }, Ids(Servico().Listar(new FiltroVendas())));
        }

        [Fact]
        public void Periodo_InclusivoNasDuasPontas()
        {
            var r = Servico().Listar(new FiltroVendas { PeriodoInicioUtc = D0.AddDays(4), PeriodoFimUtc = D0.AddDays(12) });
            Assert.Equal(new[] { "V4", "V3", "V2" }, Ids(r));
        }

        [Fact]
        public void Periodo_SoInicio_SoFim()
        {
            Assert.Equal(new[] { "V4", "V3" }, Ids(Servico().Listar(new FiltroVendas { PeriodoInicioUtc = D0.AddDays(8) })).Where(i => i != "V5").ToArray());
            Assert.Equal(new[] { "V2", "V1" }, Ids(Servico().Listar(new FiltroVendas { PeriodoFimUtc = D0.AddDays(4) })));
        }

        [Fact]
        public void Cliente_ExatoPorPadrao()
        {
            Assert.Equal(new[] { "V2" }, Ids(Servico().Listar(new FiltroVendas { ClienteId = "CLI-002" })));
            Assert.Empty(Servico().Listar(new FiltroVendas { ClienteId = "CLI-00" }).Itens);
        }

        [Fact]
        public void Cliente_Contem()
        {
            var r = Servico().Listar(new FiltroVendas { ClienteId = "CLI-002", ClienteContem = true });
            Assert.Equal(new[] { "V3", "V2" }, Ids(r));
        }

        [Fact]
        public void Status_Isolado()
        {
            Assert.Equal(new[] { "V4", "V1" }, Ids(Servico().Listar(new FiltroVendas { Status = StatusVenda.Quitada })));
            Assert.Equal(new[] { "V3", "V2" }, Ids(Servico().Listar(new FiltroVendas { Status = StatusVenda.Pendente })));
        }

        [Fact]
        public void Combinados_ApenasQuemAtendeTodos()
        {
            var r = Servico().Listar(new FiltroVendas
            {
                ClienteId = "CLI-001",
                Status = StatusVenda.Quitada,
                PeriodoInicioUtc = D0.AddDays(5)
            });
            Assert.Equal(new[] { "V4" }, Ids(r));
        }

        [Fact]
        public void PeriodoInvertido_LancaValidacaoException_EmListarEListarParaRelatorio()
        {
            var f = new FiltroVendas { PeriodoInicioUtc = D0.AddDays(2), PeriodoFimUtc = D0 };
            var ex = Assert.Throws<ValidacaoException>(() => Servico().Listar(f));
            Assert.Equal("PERIODO_INVALIDO", ex.Erro.Codigo);
            Assert.Throws<ValidacaoException>(() => Servico().ListarParaRelatorio(f));
        }

        [Fact]
        public void Relatorio_AplicaMesmoFiltro_TotalBate()
        {
            var r = Servico().ListarParaRelatorio(new FiltroVendas { Status = StatusVenda.Quitada });
            Assert.Equal(2, r.Linhas.Count);
            Assert.Equal(200m, r.TotalListado);
        }

        [Fact]
        public void Limite5000_PreservadoComFiltro()
        {
            var muitas = Enumerable.Range(0, ConsultaService.LimiteListagem + 5)
                .Select(i => Pend("X" + i, "CLI-900", D0.AddMinutes(i + 1)));
            var r = Servico(muitas).Listar(new FiltroVendas { ClienteId = "CLI-900" });
            Assert.Equal(ConsultaService.LimiteListagem, r.Itens.Count);
            Assert.True(r.Truncado);
            Assert.Equal("X" + (ConsultaService.LimiteListagem + 4), r.Itens[0].VendaId);
        }
    }
}
