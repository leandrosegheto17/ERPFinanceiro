using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Enums;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// T-58 (F-2): lógica do painel de filtros e painel headless (STA, sem Show()), sem Firebird.
    /// Renderização/foco/Enter reais NÃO são cobertos (conferência manual).
    /// </summary>
    public class FiltrosConsultaTests
    {
        // UTC-3 fixo, sem horário de verão: torna a conversão local->UTC determinística.
        private static readonly TimeZoneInfo Fuso = TimeZoneInfo.CreateCustomTimeZone("T58", TimeSpan.FromHours(-3), "T58", "T58");

        private static void ExecutarSta(Action acao)
        {
            Exception erro = null;
            var t = new Thread(() => { try { acao(); } catch (Exception ex) { erro = ex; } });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            if (!t.Join(TimeSpan.FromSeconds(60))) throw new TimeoutException("Teste de UI headless excedeu 60s.");
            if (erro != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(erro).Throw();
        }

        private static void Bombear(Task t)
        {
            var limite = Stopwatch.StartNew();
            while (!t.IsCompleted && limite.Elapsed < TimeSpan.FromSeconds(30))
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(10);
            }
            System.Windows.Forms.Application.DoEvents();
            Assert.True(t.IsCompleted, "Tarefa não terminou em 30s.");
        }

        // ---------- Lógica (sem UI) ----------

        [Fact]
        public void Montar_ConverteDatasLocaisParaUtc_FimInclusivoAteFimDoDia()
        {
            var m = new FiltrosConsultaModelo(Fuso);
            FiltroVendas f = m.Montar(new CamposFiltro
            {
                DataInicial = new DateTime(2026, 9, 1),
                DataFinal = new DateTime(2026, 9, 30, 14, 0, 0), // hora do DateEdit é ignorada
                Cliente = "  C-1 ",
                Status = StatusVenda.Quitada
            });
            Assert.Equal(new DateTime(2026, 9, 1, 3, 0, 0), f.PeriodoInicioUtc);
            // fim = 30/09 23:59:59.9999999 local = 01/10 02:59:59.9999999 UTC
            Assert.Equal(new DateTime(2026, 10, 1, 3, 0, 0).AddTicks(-1), f.PeriodoFimUtc);
            Assert.Equal("C-1", f.ClienteId);
            Assert.True(f.ClienteContem);
            Assert.Equal(StatusVenda.Quitada, f.Status);
        }

        [Fact]
        public void Montar_CamposVazios_SemCriterios()
        {
            var m = new FiltrosConsultaModelo(Fuso);
            FiltroVendas f = m.Montar(new CamposFiltro { Cliente = "   " });
            Assert.True(f.SemCriterios);
            Assert.False(f.ClienteContem);
        }

        [Fact]
        public void Aplicar_PeriodoInvalido_DevolveMensagemInline_ENaoAlteraFiltroCorrente()
        {
            var m = new FiltrosConsultaModelo(Fuso);
            m.Aplicar(new CamposFiltro { Cliente = "X" });
            FiltroVendas antes = m.Corrente;

            ResultadoAplicarFiltro r = m.Aplicar(new CamposFiltro { DataInicial = new DateTime(2026, 9, 10), DataFinal = new DateTime(2026, 9, 1) });

            Assert.False(r.Valido);
            Assert.Equal("O período inicial não pode ser posterior ao período final.", r.ErroPeriodo);
            Assert.Same(antes, m.Corrente);
        }

        [Fact]
        public void Aplicar_MesmoDia_EhValido()
        {
            var m = new FiltrosConsultaModelo(Fuso);
            Assert.True(m.Aplicar(new CamposFiltro { DataInicial = new DateTime(2026, 9, 5), DataFinal = new DateTime(2026, 9, 5) }).Valido);
            Assert.True(m.Corrente.PeriodoInicioUtc < m.Corrente.PeriodoFimUtc);
        }

        [Fact]
        public void Limpar_VoltaAoFiltroVazio_EResumoPadrao()
        {
            var m = new FiltrosConsultaModelo(Fuso);
            m.Aplicar(new CamposFiltro { Cliente = "X", Status = StatusVenda.Pendente });
            Assert.True(m.FiltroAtivo);
            m.Limpar();
            Assert.False(m.FiltroAtivo);
            Assert.True(m.Corrente.SemCriterios);
            Assert.Equal(TextosFiltros.SemFiltros, m.Resumo);
            Assert.Equal(TextosConsulta.Vazio, m.TextoVazio);
        }

        [Fact]
        public void Resumo_EstadoVazio_TextosDeF2()
        {
            var m = new FiltrosConsultaModelo(Fuso);
            m.Aplicar(new CamposFiltro { DataInicial = new DateTime(2026, 9, 1), Cliente = "abc", Status = StatusVenda.Cancelada });
            Assert.Equal("Filtros: período 01/09/2026 a ...; cliente contém \"abc\"; status Cancelada", m.Resumo);
            Assert.Equal("Nenhuma venda para os filtros informados.", m.TextoVazio);
        }

        [Fact]
        public void Relatorio_UsaOFiltroCorrenteDoPainel()
        {
            var m = new FiltrosConsultaModelo(Fuso);
            m.Aplicar(new CamposFiltro { Cliente = "C-9", Status = StatusVenda.Quitada });
            FiltroVendas recebido = null;
            string pasta = Path.Combine(Path.GetTempPath(), "T58_" + Guid.NewGuid().ToString("N"));
            try
            {
                var fluxo = new FluxoEmitirRelatorio(
                    f => { recebido = f; return new ResultadoRelatorioVendas(new List<LinhaRelatorio>(), 0m, 0); },
                    (r, f, e, caminho) => File.WriteAllText(caminho, "x"),
                    () => pasta, new AbridorNulo(), new ApresentadorPreviewIndisponivel(), () => DateTime.Now, true, null);
                Func<FiltroVendas> filtroCorrente = () => m.Corrente; // mesma ligação de Program/ComposicaoJanelaPrincipal
                fluxo.Emitir(filtroCorrente(), CancellationToken.None);
                Assert.Same(m.Corrente, recebido);
                Assert.Equal("C-9", recebido.ClienteId);
                Assert.Equal(StatusVenda.Quitada, recebido.Status);
            }
            finally { try { Directory.Delete(pasta, true); } catch (IOException) { } }
        }

        private sealed class AbridorNulo : IAbridorArquivo { public void Abrir(string c) { } }

        // ---------- Painel headless ----------

        private sealed class Ambiente
        {
            public List<FiltroVendas> Consultas = new List<FiltroVendas>();
            public List<string> Barra = new List<string>();
            public FiltrosConsultaModelo Modelo = new FiltrosConsultaModelo(Fuso);
            public FrmConsulta Janela;
            public PainelFiltrosVisual Painel;
            public int Retorno;

            public Ambiente(int itens = 0)
            {
                Retorno = itens;
                Janela = new FrmConsulta(new ConsultaVendasPresenter(() =>
                {
                    Consultas.Add(Modelo.Corrente);
                    var lista = new List<VendaListagemDto>();
                    for (int i = 0; i < Retorno; i++)
                        lista.Add(new VendaListagemDto { VendaId = "V" + i, ClienteId = "C", ValorTotal = 1m, DataRecebimento = DateTime.UtcNow });
                    return new ResultadoListagemVendas(lista, false);
                }));
                Painel = new PainelFiltrosVisual(Janela, Modelo, t => Barra.Add(t));
            }
        }

        [Fact]
        public void Painel_Aplicar_MontaFiltroDosCampos_ConsultaComEle_EMostraResumoNaBarra()
        {
            ExecutarSta(() =>
            {
                var a = new Ambiente(2);
                using (a.Janela)
                {
                    a.Painel.DataInicial.EditValue = new DateTime(2026, 9, 1);
                    a.Painel.Cliente.Text = "C-1";
                    a.Painel.Status.SelectedIndex = 2; // Pendente
                    Bombear(a.Painel.AplicarAsync());

                    Assert.Single(a.Consultas);
                    Assert.Equal("C-1", a.Consultas[0].ClienteId);
                    Assert.Equal(StatusVenda.Pendente, a.Consultas[0].Status);
                    Assert.NotNull(a.Consultas[0].PeriodoInicioUtc);
                    Assert.Contains("cliente contém \"C-1\"", a.Barra[a.Barra.Count - 1]);
                    Assert.Equal(EstadoConsulta.Dados, a.Janela.Estado);
                    Assert.Null(a.Painel.TextoErro);
                }
            });
        }

        [Fact]
        public void Painel_PeriodoInvalido_MostraMensagemInline_SemConsultar()
        {
            ExecutarSta(() =>
            {
                var a = new Ambiente();
                using (a.Janela)
                {
                    a.Painel.DataInicial.EditValue = new DateTime(2026, 9, 10);
                    a.Painel.DataFinal.EditValue = new DateTime(2026, 9, 1);
                    Bombear(a.Painel.AplicarAsync());

                    Assert.Empty(a.Consultas);
                    Assert.Equal(TextosFiltros.PeriodoInvalido, a.Painel.TextoErro);
                }
            });
        }

        [Fact]
        public void Painel_SemResultado_MostraTextoVazioDeFiltro_ELimparRecarregaSemFiltro()
        {
            ExecutarSta(() =>
            {
                var a = new Ambiente(0);
                using (a.Janela)
                {
                    a.Painel.Cliente.Text = "inexistente";
                    Bombear(a.Painel.AplicarAsync());
                    Assert.Equal(EstadoConsulta.Vazio, a.Janela.Estado);
                    Assert.Equal("Nenhuma venda para os filtros informados.", a.Janela.TextoCentralVazio);

                    a.Retorno = 3;
                    Bombear(a.Painel.LimparAsync());
                    Assert.Equal(2, a.Consultas.Count);
                    Assert.True(a.Consultas[1].SemCriterios);
                    Assert.Equal(EstadoConsulta.Dados, a.Janela.Estado);
                    Assert.Equal(string.Empty, a.Painel.Cliente.Text);
                    Assert.Equal(TextosFiltros.SemFiltros, a.Barra[a.Barra.Count - 1]);
                }
            });
        }

        [Fact]
        public void Painel_Acessibilidade_NomesAccessKeyETabAntesDaGrid()
        {
            ExecutarSta(() =>
            {
                var a = new Ambiente();
                using (a.Janela)
                {
                    Assert.Equal("Aplicar filtros", a.Painel.BotaoAplicar.AccessibleName);
                    Assert.StartsWith("&", a.Painel.BotaoAplicar.Text);
                    Assert.StartsWith("&", a.Painel.BotaoLimpar.Text);
                    Assert.Equal("Período inicial", a.Painel.DataInicial.AccessibleName);
                    Assert.Equal("Período final", a.Painel.DataFinal.AccessibleName);
                    Assert.Equal("Cliente", a.Painel.Cliente.AccessibleName);
                    Assert.Equal("Status", a.Painel.Status.AccessibleName);
                    // painel de filtros (TabIndex 0) vem antes do host da grid (TabIndex 1)
                    Assert.True(a.Painel.DataInicial.Parent.TabIndex < a.Janela.Grid.Parent.TabIndex);
                    Assert.Same(a.Janela.Grid, a.Janela.ActiveControl); // foco inicial segue na grid (T-51)
                }
            });
        }
    }
}
