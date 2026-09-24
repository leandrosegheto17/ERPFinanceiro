using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// T-51 (UX-SPEC 5/6): o que e verificavel headless (propriedades de acessibilidade, ordem de Tab, tamanhos
    /// minimos, AutoScaleMode, atalhos e contraste calculado pela paleta da skin). DPI real, Tab ao vivo e
    /// contraste renderizado NAO sao cobertos aqui (conferencia manual, T-52).
    /// </summary>
    public class AcessibilidadeTests
    {
        private sealed class FonteFake : IFonteEstadoApi
        {
            public EstadoApiHost Estado { get; set; } = EstadoApiHost.Ativa;
            public Exception UltimoErro { get; set; }
            public string EnderecoBase { get; set; } = "http://localhost:5000/";
        }

        private sealed class HealthFake : IHealthService
        {
            public ResultadoHealth ObterStatus() => ResultadoHealth.ComSucesso();
        }

        private sealed class AreaFake : IAreaTransferencia { public void Copiar(string t) { } }

        private sealed class TimerFake : ITemporizador
        {
            public event EventHandler Tick { add { } remove { } }
            public void Iniciar(TimeSpan i) { }
            public void Parar() { }
            public void Dispose() { }
        }

        private sealed class AbridorFake : IAbridorArquivo { public void Abrir(string c) { } }

        private static FrmConsulta CriarJanela(Func<ModeloConsultaVendas> carregar = null)
        {
            return new FrmConsulta(new ConsultaVendasPresenter(() =>
            {
                if (carregar != null) carregar();
                return new ResultadoListagemVendas(new VendaListagemDto[0], false);
            }));
        }

        private static void ExecutarSta(Action acao)
        {
            Exception erro = null;
            var t = new Thread(() => { try { acao(); } catch (Exception ex) { erro = ex; } });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            if (!t.Join(TimeSpan.FromSeconds(60))) throw new TimeoutException("Teste de UI headless excedeu 60s.");
            if (erro != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(erro).Throw();
        }

        // ---------- Tamanhos e escala ----------

        [Fact]
        public void JanelaPrincipal_TamanhoMinimo1024x600_AutoScaleDpi()
        {
            ExecutarSta(() =>
            {
                using (var frm = CriarJanela())
                {
                    Assert.Equal(new Size(1024, 600), frm.MinimumSize);
                    Assert.Equal(AutoScaleMode.Dpi, frm.AutoScaleMode);
                }
            });
        }

        [Fact]
        public void Dialogos_TamanhoMinimo640x420_AutoScaleDpi()
        {
            ExecutarSta(() =>
            {
                var linha = new LinhaConsultaVenda(new VendaListagemDto { VendaId = "V-1", ClienteId = "C", ValorTotal = 1m, DataRecebimento = DateTime.UtcNow });
                using (var det = new FrmDetalheVenda(new DetalheVendaPresenter(id => null), linha))
                {
                    Assert.True(det.MinimumSize.Width >= 640 && det.MinimumSize.Height >= 420);
                    Assert.Equal(AutoScaleMode.Dpi, det.AutoScaleMode);
                }
                var barra = new BarraStatusPresenter(new FonteFake(), new HealthFake(), 5000, @"C:\x.fdb", new AreaFake(), new TimerFake());
                using (var dlg = new FrmDetalhesStatus(barra))
                {
                    Assert.Equal(new Size(640, 420), dlg.MinimumSize);
                    Assert.True(dlg.Width >= 640 && dlg.Height >= 420);
                    Assert.Equal(AutoScaleMode.Dpi, dlg.AutoScaleMode);
                }
                using (var espera = new FrmAguardeRelatorio(() => { }))
                {
                    Assert.Equal(AutoScaleMode.Dpi, espera.AutoScaleMode);
                }
            });
        }

        // ---------- Nomes acessiveis e ordem de Tab ----------

        [Fact]
        public void JanelaPrincipal_GridComNomeAcessivel_EOrdemDeTabGridDepoisEmitir()
        {
            ExecutarSta(() =>
            {
                using (var frm = CriarJanela())
                {
                    var fluxo = new FluxoEmitirRelatorio(f => null, (r, f, e, c) => { }, () => System.IO.Path.GetTempPath(),
                        new AbridorFake(), new ApresentadorPreviewIndisponivel(), () => DateTime.Now, true, null);
                    var visual = new EmissaoRelatorioVisual(frm, fluxo, () => new FiltroVendas(), t => { });

                    Assert.Equal("Lista de vendas", frm.Grid.AccessibleName);
                    Assert.False(string.IsNullOrWhiteSpace(frm.Grid.AccessibleDescription));
                    Assert.Equal("Emitir relatório", visual.Botao.AccessibleName);
                    Assert.False(string.IsNullOrWhiteSpace(visual.Botao.AccessibleDescription));
                    Assert.False(string.IsNullOrWhiteSpace(frm.BotaoTentar.AccessibleName));
                    Assert.False(string.IsNullOrWhiteSpace(frm.BotaoTentar.AccessibleDescription));
                    Assert.StartsWith("&", visual.Botao.Text);
                    Assert.StartsWith("&", frm.BotaoTentar.Text);

                    var ordem = OrdemDeTab(frm);
                    int iGrid = ordem.IndexOf(frm.Grid);
                    int iTentar = ordem.IndexOf(frm.BotaoTentar);
                    int iEmitir = ordem.IndexOf(visual.Botao);
                    Assert.True(iGrid >= 0 && iTentar >= 0 && iEmitir >= 0, "controles focaveis precisam estar na ordem de Tab");
                    Assert.True(iGrid < iTentar && iTentar < iEmitir, "ordem esperada: grid -> Tentar novamente -> Emitir relatorio");
                    Assert.Same(frm.Grid, frm.ActiveControl); // foco inicial na grid
                }
            });
        }

        [Fact]
        public void Detalhe_OrdemDeTabItensTentarFechar_ENomesAcessiveis_EscFecha()
        {
            ExecutarSta(() =>
            {
                var linha = new LinhaConsultaVenda(new VendaListagemDto { VendaId = "V-1", ClienteId = "C", ValorTotal = 1m, DataRecebimento = DateTime.UtcNow });
                using (var det = new FrmDetalheVenda(new DetalheVendaPresenter(id => null), linha))
                {
                    Assert.Equal("Itens da venda", det.Grid.AccessibleName);
                    Assert.Equal("Tentar novamente", det.BotaoTentar.AccessibleName);
                    Assert.Equal("Fechar", det.BotaoFecharControle.AccessibleName);
                    Assert.False(string.IsNullOrWhiteSpace(det.BotaoFecharControle.AccessibleDescription));
                    Assert.StartsWith("&", det.BotaoTentar.Text);
                    Assert.StartsWith("&", det.BotaoFecharControle.Text);
                    Assert.Same(det.BotaoFechar, det.CancelButton); // Esc

                    var ordem = OrdemDeTab(det);
                    Assert.True(ordem.IndexOf(det.Grid) < ordem.IndexOf(det.BotaoTentar));
                    Assert.True(ordem.IndexOf(det.BotaoTentar) < ordem.IndexOf(det.BotaoFecharControle));
                }
            });
        }

        [Fact]
        public void DialogoStatus_TabMemoCopiarFechar_ENomesAcessiveis_EscFecha()
        {
            ExecutarSta(() =>
            {
                var barra = new BarraStatusPresenter(new FonteFake(), new HealthFake(), 5000, @"C:\x.fdb", new AreaFake(), new TimerFake());
                using (var dlg = new FrmDetalhesStatus(barra))
                {
                    var focaveis = OrdemDeTab(dlg);
                    var nomes = focaveis.Select(c => c.AccessibleName).ToArray();
                    Assert.Equal(new[] { "Detalhes do status", "Copiar", "Fechar" }, nomes);
                    Assert.All(focaveis, c => Assert.False(string.IsNullOrWhiteSpace(c.AccessibleDescription)));
                    Assert.NotNull(dlg.CancelButton); // Esc
                }
            });
        }

        // ---------- Atalhos ----------

        [Fact]
        public void F5_RecarregaAConsulta()
        {
            ExecutarSta(() =>
            {
                int chamadas = 0;
                using (var frm = new FrmConsulta(new ConsultaVendasPresenter(() =>
                {
                    Interlocked.Increment(ref chamadas);
                    return new ResultadoListagemVendas(new VendaListagemDto[0], false);
                })))
                {
                    var e = new KeyEventArgs(Keys.F5);
                    frm.TratarTecla(e);
                    Assert.True(e.Handled);
                    var limite = DateTime.UtcNow.AddSeconds(20);
                    while (Volatile.Read(ref chamadas) < 1 && DateTime.UtcNow < limite) { System.Windows.Forms.Application.DoEvents(); Thread.Sleep(20); }
                    Assert.True(chamadas >= 1);
                }
            });
        }

        [Fact]
        public void Enter_NaGrid_MarcaComoTratado_SemDetalhePresenterNaoQuebra()
        {
            ExecutarSta(() =>
            {
                using (var frm = CriarJanela())
                {
                    var e = new KeyEventArgs(Keys.Enter);
                    frm.TratarTeclaGrid(e);
                    Assert.True(e.Handled);
                }
            });
        }

        [Fact]
        public void F2_AbreODialogoDeDetalhesDoStatus_RL10_01()
        {
            ExecutarSta(() =>
            {
                using (var frm = CriarJanela())
                {
                    frm.AnexarBarraStatus(new BarraStatusPresenter(new FonteFake(), new HealthFake(), 5000, @"C:\x.fdb", new AreaFake(), new TimerFake()));
                    string encontrado = null;
                    var timer = new System.Windows.Forms.Timer { Interval = 100 };
                    timer.Tick += (s, ev) =>
                    {
                        var dlg = System.Windows.Forms.Application.OpenForms.OfType<FrmDetalhesStatus>().FirstOrDefault();
                        if (dlg == null) return;
                        encontrado = dlg.Text;
                        timer.Stop();
                        dlg.Close();
                    };
                    timer.Start();
                    var e = new KeyEventArgs(Keys.F2);
                    frm.TratarTecla(e); // modal: retorna depois que o timer fecha o dialogo
                    timer.Dispose();
                    Assert.True(e.Handled);
                    Assert.Equal("Detalhes do status", encontrado);
                }
            });
        }

        [Fact]
        public void F2_SemBarraDeStatus_NaoQuebra()
        {
            ExecutarSta(() =>
            {
                using (var frm = CriarJanela())
                {
                    var e = new KeyEventArgs(Keys.F2);
                    frm.TratarTecla(e);
                    Assert.True(e.Handled);
                }
            });
        }

        // ---------- Contraste (calculado pela paleta da skin, nao renderizado) ----------

        [Fact]
        public void Contraste_PaletaDaSkin_TextoNormalMaiorOuIgual4_5()
        {
            ExecutarSta(() =>
            {
                ConfiguracaoVisual.AplicarSkin();
                var cores = CommonSkins.GetSkin(UserLookAndFeel.Default).Colors;
                var pares = new[]
                {
                    Tuple.Create("WindowText/Window", cores[CommonColors.WindowText], cores[CommonColors.Window]),
                    Tuple.Create("ControlText/Control", cores[CommonColors.ControlText], cores[CommonColors.Control]),
                    Tuple.Create("DisabledText/Control (referencia)", cores[CommonColors.DisabledText], cores[CommonColors.Control]),
                };
                foreach (var p in pares.Take(2))
                {
                    double razao = Razao(p.Item2, p.Item3);
                    Assert.True(razao >= 4.5, p.Item1 + " = " + razao.ToString("0.00"));
                }
                // Texto do estado vazio usa SystemColors.GrayText sobre a cor da janela.
                double gray = Razao(SystemColors.GrayText, cores[CommonColors.Window]);
                Assert.True(gray >= 4.5, "GrayText/Window = " + gray.ToString("0.00"));
            });
        }

        private static double Razao(Color a, Color b)
        {
            double la = Luminancia(a), lb = Luminancia(b);
            double claro = Math.Max(la, lb), escuro = Math.Min(la, lb);
            return (claro + 0.05) / (escuro + 0.05);
        }

        private static double Luminancia(Color c)
        {
            double Canal(byte v)
            {
                double s = v / 255.0;
                return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Canal(c.R) + 0.7152 * Canal(c.G) + 0.0722 * Canal(c.B);
        }

        // ---------- Utilitario: ordem de Tab efetiva (TabStop, recursiva, por TabIndex) ----------

        private static List<Control> OrdemDeTab(Control raiz)
        {
            var resultado = new List<Control>();
            void Visitar(Control pai)
            {
                foreach (Control c in pai.Controls.Cast<Control>().OrderBy(x => x.TabIndex))
                {
                    if (c.TabStop && (c is DevExpress.XtraEditors.SimpleButton || c is DevExpress.XtraGrid.GridControl || c is DevExpress.XtraEditors.MemoEdit))
                        resultado.Add(c);
                    Visitar(c);
                }
            }
            Visitar(raiz);
            return resultado;
        }
    }
}
