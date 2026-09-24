using System;
using System.Windows.Forms;
using Autofac;
using DevExpress.XtraEditors;
using ERPFinanceiro.Application.Interfaces;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Composição de UI da janela principal (T-45 ligação + T-47): cria e anexa a barra de status
    /// F-6 e liga confirmação ao fechar + encerramento ordenado. Sem <c>Application.Run</c>,
    /// testável headless. A confirmação real (<c>XtraMessageBox</c>) é injetada por <see cref="Program"/>.
    /// </summary>
    public sealed class ComposicaoJanelaPrincipal
    {
        private readonly Func<bool> _confirmar;

        public ComposicaoJanelaPrincipal(FrmConsulta janela, IContainer container, ApiHost host, int porta,
            string caminhoFdb, ITemporizador temporizador, IAreaTransferencia areaTransferencia,
            Func<bool> confirmar, Action liberarInstancia = null, Action<Exception> aoFalhar = null,
            FluxoEmitirRelatorio fluxoRelatorio = null, Func<Application.Consultas.FiltroVendas> filtroCorrente = null,
            FiltrosConsultaModelo filtros = null)
        {
            if (janela == null) throw new ArgumentNullException(nameof(janela));
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (host == null) throw new ArgumentNullException(nameof(host));
            _confirmar = confirmar ?? throw new ArgumentNullException(nameof(confirmar));

            Barra = new BarraStatusPresenter(new FonteEstadoApiHost(host), container.Resolve<IHealthService>(),
                porta, caminhoFdb, areaTransferencia, temporizador);
            janela.AnexarBarraStatus(Barra); // inicia atualização (abertura + 15 s)

            if (filtros != null)
            {
                // T-58 (F-2): painel de filtros; o relatório usa o mesmo filtro corrente (T-48).
                Filtros = new PainelFiltrosVisual(janela, filtros, texto => janela.BarraStatus?.MostrarMensagem(texto));
                if (filtroCorrente == null) filtroCorrente = () => filtros.Corrente;
            }

            if (fluxoRelatorio != null)
            {
                // T-50 (F-5): botão + espera cancelável; "Relatório gerado" na barra de status.
                Relatorio = new EmissaoRelatorioVisual(janela, fluxoRelatorio, filtroCorrente,
                    texto => janela.BarraStatus?.MostrarMensagem(texto));
            }

            Encerramento = new EncerramentoAplicacao(
                pararBarra: Barra.Dispose,
                pararApi: host.Stop,
                descartarContainer: container.Dispose,
                liberarInstancia: liberarInstancia,
                aoFalhar: aoFalhar);

            janela.FormClosing += (s, e) => TratarFechando(e);
            janela.FormClosed += (s, e) => Encerramento.Encerrar();
        }

        public EmissaoRelatorioVisual Relatorio { get; }
        public PainelFiltrosVisual Filtros { get; }
        public BarraStatusPresenter Barra { get; }
        public EncerramentoAplicacao Encerramento { get; }

        /// <summary>"Não" cancela o fechamento sem alterar nada; desligamento do Windows não pergunta.</summary>
        internal void TratarFechando(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing) return;
            if (!Encerramento.PodeFechar(_confirmar)) e.Cancel = true;
        }

        /// <summary>Diálogo real (DevExpress padrão), texto literal do UX-SPEC; foco padrão em "Não" (ação segura).</summary>
        public static bool ConfirmarComXtraMessageBox()
        {
            return XtraMessageBox.Show(EncerramentoAplicacao.TextoConfirmacao, TextosInicializacao.TituloApp,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }
    }
}
