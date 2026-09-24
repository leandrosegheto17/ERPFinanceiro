using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Controle visual da barra de status F-6 (T-45, UX-SPEC 3: <c>BarManager</c> StatusBar +
    /// <c>BarStaticItem</c> ícone + texto). Toda a lógica vive em <see cref="BarraStatusPresenter"/>.
    /// </summary>
    public sealed class BarraStatusVisual : IDisposable
    {
        private readonly BarraStatusPresenter _presenter;
        private readonly BarManager _manager;
        private readonly BarStaticItem _itemApi;
        private readonly BarStaticItem _itemBanco;
        private readonly BarStaticItem _itemMensagem;
        private readonly DevExpress.Utils.SvgImageCollection _icones;

        public BarraStatusVisual(Form formulario, BarraStatusPresenter presenter)
        {
            if (formulario == null) throw new ArgumentNullException(nameof(formulario));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _icones = ConfiguracaoVisual.CriarIconesStatus();

            _manager = new BarManager { Form = formulario };
            var bar = new Bar(_manager, "Status") { DockStyle = BarDockStyle.Bottom };
            bar.OptionsBar.AllowQuickCustomization = false;
            bar.OptionsBar.DrawDragBorder = false;
            bar.OptionsBar.UseWholeRow = true;
            _manager.StatusBar = bar;

            _itemApi = CriarItem(bar, "Status da API");
            _itemBanco = CriarItem(bar, "Status do banco");
            _itemMensagem = CriarItem(bar, "Mensagem");
            _itemApi.ItemClick += (s, e) => AoClicar();
            _itemBanco.ItemClick += (s, e) => AoClicar();

            _presenter.Alterado += (s, e) => Refletir();
            Refletir();
        }

        /// <summary>T-50: mensagem transitória ("Relatório gerado") ao lado dos indicadores.</summary>
        public void MostrarMensagem(string texto)
        {
            _itemMensagem.Caption = texto ?? string.Empty;
        }

        internal string TextoMensagem => _itemMensagem.Caption;
        internal string TextoApi => _itemApi.Caption;
        internal string TextoBanco => _itemBanco.Caption;
        internal bool ApiTemIcone => _itemApi.ImageOptions.SvgImage != null;
        internal bool BancoTemIcone => _itemBanco.ImageOptions.SvgImage != null;

        private BarStaticItem CriarItem(Bar bar, string nomeAcessivel)
        {
            var item = new BarStaticItem { Caption = string.Empty, AllowRightClickInMenu = false };
            item.Description = nomeAcessivel;
            _manager.Items.Add(item);
            bar.AddItem(item);
            return item;
        }

        private void Refletir()
        {
            Aplicar(_itemApi, _presenter.Api);
            Aplicar(_itemBanco, _presenter.Banco);
        }

        private void Aplicar(BarStaticItem item, IndicadorStatus indicador)
        {
            item.Caption = indicador.Texto;
            item.ImageOptions.SvgImage = _icones[(int)indicador.Icone];
        }

        /// <summary>Clique em qualquer indicador: atualiza e abre o diálogo de detalhes.</summary>
        internal void AoClicar()
        {
            var _ = _presenter.AtualizarAsync();
            using (var dialogo = new FrmDetalhesStatus(_presenter))
            {
                dialogo.ShowDialog(_manager.Form as IWin32Window);
            }
        }

        public void Dispose()
        {
            _presenter.Dispose();
            _manager.Dispose();
        }
    }

    /// <summary>Diálogo de detalhes do status: porta, caminho do .fdb, último erro e botão Copiar (T-45).</summary>
    public sealed class FrmDetalhesStatus : XtraForm
    {
        private readonly BarraStatusPresenter _presenter;
        private readonly MemoEdit _memo;

        public FrmDetalhesStatus(BarraStatusPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            Text = "Detalhes do status";
            AutoScaleMode = AutoScaleMode.Dpi; // T-51
            Size = new Size(640, 420);
            MinimumSize = new Size(640, 420);  // UX-SPEC 6 (diálogos)
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;

            _memo = new MemoEdit { Dock = DockStyle.Fill, AccessibleName = "Detalhes do status", AccessibleDescription = "Porta, caminho do banco e último erro. Somente leitura." };
            _memo.Properties.ReadOnly = true;
            _memo.Text = presenter.TextoDetalhes;

            var btnCopiar = new SimpleButton { Text = "&Copiar", Size = new Size(100, 28), AccessibleName = "Copiar", AccessibleDescription = "Copia os detalhes para a área de transferência." };
            btnCopiar.Click += (s, e) => Copiar();
            var btnFechar = new SimpleButton { Text = "&Fechar", Size = new Size(100, 28), AccessibleName = "Fechar", AccessibleDescription = "Fecha o diálogo. Atalho: Esc.", DialogResult = DialogResult.Cancel };

            var rodape = new PanelControl { Dock = DockStyle.Bottom, Height = 44, BorderStyle = BorderStyles.NoBorder };
            btnCopiar.Location = new Point(10, 8);
            btnFechar.Location = new Point(120, 8);
            rodape.Controls.Add(btnCopiar);
            rodape.Controls.Add(btnFechar);

            Controls.Add(_memo);
            Controls.Add(rodape);
            CancelButton = btnFechar;
            _memo.TabIndex = 0;
            rodape.TabIndex = 1;
            btnCopiar.TabIndex = 0;
            btnFechar.TabIndex = 1;
            ActiveControl = btnCopiar;
        }

        internal string TextoExibido => _memo.Text;

        internal void Copiar() => _presenter.CopiarDetalhes();
    }
}
