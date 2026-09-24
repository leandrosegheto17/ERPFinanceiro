using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraWaitForm;
using ERPFinanceiro.Application.Consultas;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Ligação visual do fluxo F-5 (T-50) à janela principal: botão "Emitir relatório" (AccessKey Alt+E),
    /// diálogo de espera cancelável (ProgressPanel + Cancelar), <c>XtraMessageBox</c> nos erros e
    /// mensagem "Relatório gerado" na barra de status. Toda a lógica está em
    /// <see cref="FluxoEmitirRelatorio"/>; aqui só há controles DevExpress padrão (regra 12).
    /// </summary>
    public sealed class EmissaoRelatorioVisual
    {
        private readonly Form _janela;
        private readonly FluxoEmitirRelatorio _fluxo;
        private readonly Func<FiltroVendas> _filtroCorrente;
        private readonly Action<string> _mensagemStatus;
        private readonly SimpleButton _botao;
        private bool _emitindo;

        public EmissaoRelatorioVisual(Form janela, FluxoEmitirRelatorio fluxo, Func<FiltroVendas> filtroCorrente, Action<string> mensagemStatus)
        {
            _janela = janela ?? throw new ArgumentNullException(nameof(janela));
            _fluxo = fluxo ?? throw new ArgumentNullException(nameof(fluxo));
            _filtroCorrente = filtroCorrente ?? (() => new FiltroVendas());
            _mensagemStatus = mensagemStatus;

            _botao = new SimpleButton
            {
                Text = TextosRelatorio.Botao,
                Size = new Size(150, 28),
                Location = new Point(8, 6),
                AccessibleName = "Emitir relatório",
                AccessibleDescription = "Gera o relatório financeiro com o filtro aplicado. Atalho: Alt+E.",
                TabIndex = 0
            };
            _botao.Click += async (s, e) => await EmitirAsync();
            var painel = new PanelControl { Dock = DockStyle.Top, Height = 40, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            painel.TabStop = false;
            painel.TabIndex = 1; // T-51: após a grid (TabIndex 0 no host de FrmConsulta)
            painel.Controls.Add(_botao);
            _janela.Controls.Add(painel);
        }

        internal SimpleButton Botao => _botao;
        internal bool Emitindo => _emitindo;

        public async Task EmitirAsync()
        {
            if (_emitindo) return;
            _emitindo = true;
            _botao.Enabled = false;
            try
            {
                using (var cts = new CancellationTokenSource())
                using (var espera = new FrmAguardeRelatorio(cts.Cancel))
                {
                    Task<ResultadoEmissaoRelatorio> tarefa = _fluxo.EmitirAsync(_filtroCorrente(), cts.Token);
                    var ui = TaskScheduler.FromCurrentSynchronizationContext();
                    var fechar = tarefa.ContinueWith(_ => { if (espera.IsHandleCreated) espera.Close(); }, ui);
                    espera.Shown += (s, e) => { if (tarefa.IsCompleted) espera.Close(); };
                    espera.ShowDialog(_janela);
                    ResultadoEmissaoRelatorio resultado = await tarefa;
                    await fechar;
                    Apresentar(resultado);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(_janela, "Não foi possível emitir o relatório: " + ex.Message, TextosRelatorio.TituloDialogo,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _botao.Enabled = true;
                _emitindo = false;
            }
        }

        private void Apresentar(ResultadoEmissaoRelatorio r)
        {
            switch (r.Tipo)
            {
                case TipoResultadoRelatorio.PdfAberto:
                case TipoResultadoRelatorio.PreviewExibido:
                    _mensagemStatus?.Invoke(TextosRelatorio.StatusGerado);
                    break;
                case TipoResultadoRelatorio.PdfGeradoSemAbrir:
                    _mensagemStatus?.Invoke(TextosRelatorio.StatusGerado);
                    XtraMessageBox.Show(_janela, r.Mensagem, TextosRelatorio.TituloDialogo, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case TipoResultadoRelatorio.Cancelado:
                    break;
                default:
                    XtraMessageBox.Show(_janela, r.Mensagem, TextosRelatorio.TituloDialogo, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }
    }

    /// <summary>Espera "Gerando relatório..." cancelável (botão Cancelar e Esc). Sem <c>Application.Run</c>.</summary>
    public sealed class FrmAguardeRelatorio : XtraForm
    {
        private readonly Action _aoCancelar;
        internal SimpleButton BotaoCancelar { get; }
        internal ProgressPanel Progresso { get; }

        public FrmAguardeRelatorio(Action aoCancelar)
        {
            _aoCancelar = aoCancelar;
            Text = TextosRelatorio.TituloDialogo;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi; // T-51
            ControlBox = false;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(300, 110);
            KeyPreview = true;

            Progresso = new ProgressPanel { Caption = TextosRelatorio.Aguarde, Description = string.Empty, Location = new Point(20, 10), Size = new Size(260, 50), AccessibleName = TextosRelatorio.Aguarde };
            BotaoCancelar = new SimpleButton { Text = "&" + TextosRelatorio.Cancelar, Location = new Point(100, 70), Size = new Size(100, 28), AccessibleName = TextosRelatorio.Cancelar, AccessibleDescription = "Cancela a geração do relatório. Atalho: Esc.", TabIndex = 0 };
            BotaoCancelar.Click += (s, e) => Cancelar();
            Controls.Add(Progresso);
            Controls.Add(BotaoCancelar);
            CancelButton = BotaoCancelar;
        }

        internal void Cancelar()
        {
            BotaoCancelar.Enabled = false;
            _aoCancelar?.Invoke(); // o fluxo devolve Cancelado; o diálogo fecha ao terminar a tarefa
        }
    }
}
