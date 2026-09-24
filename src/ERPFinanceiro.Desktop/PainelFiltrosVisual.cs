using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Painel de filtros de F-2 (T-58, UX-SPEC 2.1/4/5). Só controles DevExpress padrão (regra 12); a lógica
    /// (montagem/validação/resumo) vive em <see cref="FiltrosConsultaModelo"/>. Enter em qualquer campo aplica;
    /// Alt+A aplica, Alt+L limpa. Período invertido mostra mensagem inline e NÃO consulta.
    /// </summary>
    public sealed class PainelFiltrosVisual
    {
        private const string ItemTodos = "Todos";

        private readonly FrmConsulta _janela;
        private readonly FiltrosConsultaModelo _modelo;
        private readonly Action<string> _resumoNaBarra;
        private readonly DateEdit _de, _ate;
        private readonly TextEdit _cliente;
        private readonly ComboBoxEdit _status;
        private readonly SimpleButton _btnAplicar, _btnLimpar;
        private readonly LabelControl _lblErro;
        private readonly PanelControl _painel;

        public PainelFiltrosVisual(FrmConsulta janela, FiltrosConsultaModelo modelo, Action<string> resumoNaBarra)
        {
            _janela = janela ?? throw new ArgumentNullException(nameof(janela));
            _modelo = modelo ?? throw new ArgumentNullException(nameof(modelo));
            _resumoNaBarra = resumoNaBarra;

            _de = CriarData("Período inicial", 8);
            _ate = CriarData("Período final", 130);
            _cliente = new TextEdit { Location = new Point(252, 22), Size = new Size(150, 22), TabIndex = 2, AccessibleName = "Cliente", AccessibleDescription = "Cliente contém o texto informado." };
            _status = new ComboBoxEdit { Location = new Point(412, 22), Size = new Size(110, 22), TabIndex = 3, AccessibleName = "Status", AccessibleDescription = "Filtra por status da venda." };
            _status.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;
            _status.Properties.Items.AddRange(new object[] { ItemTodos, "Quitada", "Pendente", "Cancelada" });
            _status.SelectedIndex = 0;
            _btnAplicar = new SimpleButton { Text = "&Aplicar", Location = new Point(532, 20), Size = new Size(80, 26), TabIndex = 4, AccessibleName = "Aplicar filtros", AccessibleDescription = "Aplica os filtros à lista. Atalho: Enter ou Alt+A." };
            _btnLimpar = new SimpleButton { Text = "&" + TextosFiltros.LimparFiltros, Location = new Point(618, 20), Size = new Size(110, 26), TabIndex = 5, AccessibleName = TextosFiltros.LimparFiltros, AccessibleDescription = "Remove todos os filtros. Atalho: Alt+L." };
            _lblErro = new LabelControl { Location = new Point(8, 50), AutoSizeMode = LabelAutoSizeMode.None, Size = new Size(720, 18), Visible = false, AccessibleName = "Erro no período" };
            _lblErro.Appearance.ForeColor = Color.Firebrick;

            _painel = new PanelControl { Dock = DockStyle.Top, Height = 72, BorderStyle = BorderStyles.NoBorder, TabStop = false, TabIndex = 0, AccessibleName = "Filtros" };
            _painel.Controls.Add(Rotulo("Período inicial", 8));
            _painel.Controls.Add(Rotulo("Período final", 130));
            _painel.Controls.Add(Rotulo("Cliente", 252));
            _painel.Controls.Add(Rotulo("Status", 412));
            _painel.Controls.Add(_de);
            _painel.Controls.Add(_ate);
            _painel.Controls.Add(_cliente);
            _painel.Controls.Add(_status);
            _painel.Controls.Add(_btnAplicar);
            _painel.Controls.Add(_btnLimpar);
            _painel.Controls.Add(_lblErro);

            foreach (Control c in new Control[] { _de, _ate, _cliente, _status })
                c.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; Aplicar(); } };
            _btnAplicar.Click += (s, e) => Aplicar();
            _btnLimpar.Click += (s, e) => Limpar();

            janela.AnexarPainelFiltros(_painel, () => _modelo.TextoVazio);
        }

        private static DateEdit CriarData(string nome, int x)
        {
            var d = new DateEdit { Location = new Point(x, 22), Size = new Size(114, 22), AccessibleName = nome, AccessibleDescription = nome + " (dd/mm/aaaa). Vazio = sem limite." };
            d.Properties.NullText = string.Empty;
            d.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            d.TabIndex = x < 100 ? 0 : 1;
            return d;
        }

        private static LabelControl Rotulo(string texto, int x)
            => new LabelControl { Text = texto, Location = new Point(x, 4), TabStop = false };

        internal CamposFiltro LerCampos()
        {
            StatusVenda? status = null;
            switch (_status.Text)
            {
                case "Quitada": status = StatusVenda.Quitada; break;
                case "Pendente": status = StatusVenda.Pendente; break;
                case "Cancelada": status = StatusVenda.Cancelada; break;
            }
            return new CamposFiltro
            {
                DataInicial = _de.EditValue is DateTime di ? di : (DateTime?)null,
                DataFinal = _ate.EditValue is DateTime df ? df : (DateTime?)null,
                Cliente = _cliente.Text,
                Status = status
            };
        }

        /// <summary>Aplicar (Enter/botão): valida antes de consultar; período inválido = mensagem inline, sem consulta.</summary>
        public async void Aplicar() { await AplicarAsync(); }

        internal async Task AplicarAsync()
        {
            ResultadoAplicarFiltro r = _modelo.Aplicar(LerCampos());
            if (!r.Valido)
            {
                _lblErro.Text = r.ErroPeriodo;
                _lblErro.Visible = _erroVisivel = true;
                return;
            }
            _lblErro.Visible = _erroVisivel = false;
            _resumoNaBarra?.Invoke(_modelo.Resumo);
            await _janela.RecarregarAsync();
        }

        public async void Limpar() { await LimparAsync(); }

        internal async Task LimparAsync()
        {
            _de.EditValue = null;
            _ate.EditValue = null;
            _cliente.Text = string.Empty;
            _status.SelectedIndex = 0;
            _lblErro.Visible = _erroVisivel = false;
            _modelo.Limpar();
            _resumoNaBarra?.Invoke(_modelo.Resumo);
            await _janela.RecarregarAsync();
        }

        internal DateEdit DataInicial => _de;
        internal DateEdit DataFinal => _ate;
        internal TextEdit Cliente => _cliente;
        internal ComboBoxEdit Status => _status;
        internal SimpleButton BotaoAplicar => _btnAplicar;
        internal SimpleButton BotaoLimpar => _btnLimpar;
        // Control.Visible é false sem o form exibido; guarda-se o estado lógico.
        private bool _erroVisivel;
        internal string TextoErro => _erroVisivel ? _lblErro.Text : null;
        internal FiltrosConsultaModelo Modelo => _modelo;
    }
}
