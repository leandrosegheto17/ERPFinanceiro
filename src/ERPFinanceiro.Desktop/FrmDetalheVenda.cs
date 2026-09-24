using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;
using DevExpress.XtraWaitForm;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Janela modal F-3 (T-44, UX-SPEC 2.2). Abre imediatamente com o cabeçalho da linha; itens
    /// carregam em Task com ProgressPanel. Falha: aviso + "Tentar novamente" sem fechar.
    /// Somente controles DevExpress padrão. Sem aba Histórico (Tier C).
    /// </summary>
    public sealed class FrmDetalheVenda : XtraForm
    {
        private readonly DetalheVendaPresenter _presenter;
        private readonly LinhaConsultaVenda _linha;
        private readonly LabelControl _lblTitulo, _lblLinha1, _lblLinha2, _lblLinha3;
        private readonly XtraTabControl _abas;
        private readonly XtraTabPage _abaItens;
        private readonly GridControl _grid;
        private readonly GridView _view;
        private readonly ProgressPanel _progresso;
        private readonly LabelControl _lblAviso;
        private readonly SimpleButton _btnTentar;
        private readonly LabelControl _lblTotal;
        private readonly SimpleButton _btnFechar;
        private bool _carregando, _avisoVisivel, _tentarVisivel, _progressoVisivel; // estado lógico (Control.Visible retorna false sem handle/pai visível)

        public FrmDetalheVenda(DetalheVendaPresenter presenter, LinhaConsultaVenda linha)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _linha = linha ?? throw new ArgumentNullException(nameof(linha));

            Size = new Size(720, 460);
            MinimumSize = new Size(640, 420);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            KeyPreview = true;

            var topo = new Panel { Dock = DockStyle.Top, Height = 92 };
            _lblTitulo = new LabelControl { Location = new Point(8, 6), AutoSizeMode = LabelAutoSizeMode.None, Size = new Size(690, 20) };
            _lblTitulo.Appearance.Font = new Font(_lblTitulo.Appearance.Font, FontStyle.Bold);
            _lblLinha1 = new LabelControl { Location = new Point(8, 30), AutoSizeMode = LabelAutoSizeMode.None, Size = new Size(690, 20) };
            _lblLinha2 = new LabelControl { Location = new Point(8, 50), AutoSizeMode = LabelAutoSizeMode.None, Size = new Size(690, 20) };
            _lblLinha3 = new LabelControl { Location = new Point(8, 70), AutoSizeMode = LabelAutoSizeMode.None, Size = new Size(690, 20) };
            topo.Controls.AddRange(new Control[] { _lblTitulo, _lblLinha1, _lblLinha2, _lblLinha3 });

            var rodape = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            _btnFechar = new SimpleButton { Text = "&Fechar", Dock = DockStyle.Right, Width = 100, DialogResult = DialogResult.Cancel };
            rodape.Controls.Add(_btnFechar);
            CancelButton = _btnFechar; // Esc fecha

            _grid = new GridControl { Dock = DockStyle.Fill };
            _view = new GridView(_grid);
            _grid.MainView = _view;
            _grid.ViewCollection.Add(_view);
            _view.OptionsBehavior.Editable = false;
            _view.OptionsBehavior.ReadOnly = true;
            _view.OptionsView.ShowGroupPanel = false;
            _view.OptionsView.ColumnAutoWidth = true;
            var cProd = _view.Columns.AddVisible(nameof(LinhaItemDetalhe.ProdutoId), "Produto ID");
            var cQtd = _view.Columns.AddVisible(nameof(LinhaItemDetalhe.Quantidade), "Quantidade");
            var cPreco = _view.Columns.AddVisible(nameof(LinhaItemDetalhe.PrecoUnitario), "Preço unit.");
            var cSub = _view.Columns.AddVisible(nameof(LinhaItemDetalhe.Subtotal), "Subtotal");
            foreach (var c in new[] { cQtd, cPreco, cSub })
            {
                c.AppearanceCell.TextOptions.HAlignment = HorzAlignment.Far;
                c.AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Far;
            }
            foreach (var c in new[] { cProd, cQtd, cPreco, cSub }) c.OptionsColumn.AllowEdit = false;

            _progresso = new ProgressPanel { Dock = DockStyle.Top, Height = 48, Visible = false, Description = "Carregando itens..." };
            _lblAviso = new LabelControl { Dock = DockStyle.Top, Height = 24, Visible = false, AutoSizeMode = LabelAutoSizeMode.None };
            _btnTentar = new SimpleButton { Text = "&Tentar novamente", Dock = DockStyle.Top, Height = 28, Visible = false };
            _btnTentar.Click += (s, e) => Carregar();
            _lblTotal = new LabelControl { Dock = DockStyle.Bottom, Height = 22, AutoSizeMode = LabelAutoSizeMode.None };
            _lblTotal.Appearance.TextOptions.HAlignment = HorzAlignment.Far;

            _abas = new XtraTabControl { Dock = DockStyle.Fill };
            _abaItens = new XtraTabPage { Text = "Itens" };
            _abaItens.Controls.Add(_grid);
            _abaItens.Controls.Add(_lblTotal);
            _abaItens.Controls.Add(_btnTentar);
            _abaItens.Controls.Add(_lblAviso);
            _abaItens.Controls.Add(_progresso);
            _abas.TabPages.Add(_abaItens);

            Controls.Add(_abas);
            Controls.Add(rodape);
            Controls.Add(topo);

            AplicarCabecalho(CabecalhoDetalheVenda.De(_linha));
            Text = "Venda " + _linha.VendaId;
            Shown += (s, e) => Carregar();
        }

        // --- estado observável (testes) ---
        internal GridView View => _view;
        internal string TextoTitulo => _lblTitulo.Text;
        internal string TextoLinha1 => _lblLinha1.Text;
        internal string TextoLinha2 => _lblLinha2.Text;
        internal string TextoLinha3 => _lblLinha3.Text;
        internal string TextoTotal => _lblTotal.Text;
        internal string TextoAviso => _avisoVisivel ? _lblAviso.Text : null;
        internal bool TentarNovamenteVisivel => _tentarVisivel;
        internal bool CarregandoVisivel => _progressoVisivel;
        internal int QuantidadeAbas => _abas.TabPages.Count;
        internal IButtonControl BotaoFechar => _btnFechar;

        private void AplicarCabecalho(CabecalhoDetalheVenda c)
        {
            _lblTitulo.Text = c.Titulo;
            _lblLinha1.Text = "Cliente: " + c.Cliente + "   Status: " + c.Status + "   Valor: " + c.Valor;
            _lblLinha2.Text = "Recebida: " + c.Recebida + "   Quitada: " + c.Quitada;
            _lblLinha3.Text = "Cancelamento/Motivo: " + c.CancelamentoMotivo;
        }

        /// <summary>Carga dos itens; não bloqueia a UI thread (regra 5). Reaproveitada por "Tentar novamente".</summary>
        public async void Carregar()
        {
            if (_carregando) return;
            _carregando = true;
            _lblAviso.Visible = _avisoVisivel = false;
            _btnTentar.Visible = _tentarVisivel = false;
            _progresso.Visible = _progressoVisivel = true;
            try
            {
                ModeloDetalheVenda modelo = await _presenter.CarregarAsync(_linha.VendaId);
                Aplicar(modelo);
            }
            catch (Exception)
            {
                MostrarErro();
            }
            finally
            {
                _progresso.Visible = _progressoVisivel = false;
                _carregando = false;
            }
        }

        internal void Aplicar(ModeloDetalheVenda modelo)
        {
            AplicarCabecalho(modelo.Cabecalho);
            _lblAviso.Visible = _avisoVisivel = false;
            _btnTentar.Visible = _tentarVisivel = false;
            _progresso.Visible = _progressoVisivel = false;
            _grid.DataSource = modelo.Itens;
            _lblTotal.Text = modelo.TotalItensTexto;
            if (modelo.SemItens)
            {
                _lblAviso.Text = ModeloDetalheVenda.TextoSemItens; // estado vazio (não é erro)
                _lblAviso.Visible = _avisoVisivel = true;
            }
        }

        internal void MostrarErro()
        {
            _lblAviso.Text = DetalheVendaPresenter.TextoErro;
            _lblAviso.Visible = _avisoVisivel = true;
            _btnTentar.Visible = _tentarVisivel = true;
            _progresso.Visible = _progressoVisivel = false;
        }
    }
}
