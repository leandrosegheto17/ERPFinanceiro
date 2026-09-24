using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.Data;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraWaitForm;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Janela principal F-1 (T-42, UX-SPEC 2.1). Somente controles DevExpress padrão (regra 12).
    /// Toda a lógica (linhas, "—", "Cancelada*", contador, ordenação) vive em
    /// <see cref="ConsultaVendasPresenter"/>/<see cref="LinhaConsultaVenda"/>; aqui só há
    /// configuração de controles. Estados vazio/carregando/erro/truncado são T-43; painel de
    /// filtros é T-58 (Tier B); barra de status é T-45.
    /// </summary>
    public sealed class FrmConsulta : XtraForm
    {
        public const string NomeColunaCliente = nameof(LinhaConsultaVenda.ClienteId);

        private readonly ConsultaVendasPresenter _presenter;
        private readonly GridControl _grid;
        private readonly GridView _view;
        private readonly LabelControl _lblContador;
        private readonly LabelControl _lblLegenda;
        private readonly SvgImageCollection _icones;
        private bool _carregando;
        private EstadoConsulta _estado = EstadoConsulta.Inicial;
        private readonly LabelControl _lblAviso;
        private readonly ProgressPanel _progresso;
        private readonly PanelControl _painelErro;
        private readonly LabelControl _lblErro;
        private readonly SimpleButton _btnTentar;
        private readonly PanelControl _host;
        private readonly PanelControl _rodape;
        private Func<string> _textoVazio;

        private void CentralizarSobreposicoes()
        {
            foreach (Control c in new Control[] { _progresso, _painelErro })
            {
                if (c.Parent == null) continue;
                c.Location = new Point(Math.Max(0, (c.Parent.Width - c.Width) / 2), Math.Max(0, (c.Parent.Height - c.Height) / 2));
            }
        }

        public FrmConsulta(ConsultaVendasPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));

            Text = "ERP Financeiro - Consulta de Vendas";
            AutoScaleMode = AutoScaleMode.Dpi; // T-51: DPI 125/150% (UX-SPEC 5)
            Size = new Size(1024, 600);
            MinimumSize = new Size(1024, 600); // UX-SPEC 6
            KeyPreview = true;

            _icones = ConfiguracaoVisual.CriarIconesStatus();

            _grid = new GridControl { Dock = DockStyle.Fill };
            _view = new GridView(_grid);
            _grid.MainView = _view;
            _grid.ViewCollection.Add(_view);
            ConfigurarView();

            _lblContador = CriarRotulo(DockStyle.Right, HorzAlignment.Far, "0 vendas");
            _lblContador.Width = 140;
            _lblContador.AccessibleName = "Contador de vendas";
            _lblLegenda = CriarRotulo(DockStyle.Left, HorzAlignment.Near, "* Cancelada sem quitação registrada: sem cliente/valor (\"—\").");
            _lblLegenda.Width = 700;
            _lblLegenda.AccessibleName = "Legenda da lista";
            var rodape = _rodape = new PanelControl { Dock = DockStyle.Bottom, Height = 28, BorderStyle = BorderStyles.NoBorder };
            _lblAviso = CriarRotulo(DockStyle.Fill, HorzAlignment.Center, string.Empty);
            _lblAviso.AccessibleName = "Aviso de limite de linhas";
            rodape.Controls.Add(_lblAviso);
            rodape.Controls.Add(_lblContador);
            rodape.Controls.Add(_lblLegenda);

            _view.CustomDrawEmptyForeground += DesenharVazio;

            _progresso = new ProgressPanel
            {
                Caption = TextosConsulta.Carregando,
                Description = string.Empty,
                Size = new Size(260, 60),
                Anchor = AnchorStyles.None,
                Visible = false,
                AccessibleName = TextosConsulta.Carregando
            };
            _lblErro = new LabelControl { Text = TextosConsulta.Erro, AutoSizeMode = LabelAutoSizeMode.None, Size = new Size(300, 20), Location = new Point(10, 10), AccessibleName = "Erro ao consultar" };
            _btnTentar = new SimpleButton { Text = "&" + TextosConsulta.BotaoTentarNovamente, Location = new Point(10, 40), Size = new Size(140, 28), AccessibleName = TextosConsulta.BotaoTentarNovamente };
            _btnTentar.Click += (s, e) => Recarregar();
            _painelErro = new PanelControl { Size = new Size(320, 80), Visible = false, Anchor = AnchorStyles.None };
            _painelErro.Controls.Add(_lblErro);
            _painelErro.Controls.Add(_btnTentar);
            // Sobreposições ficam num host irmão da grid: Enabled=false da grid (esmaecida) não desabilita o botão.
            var host = _host = new PanelControl { Dock = DockStyle.Fill, BorderStyle = BorderStyles.NoBorder };
            host.Controls.Add(_progresso);
            host.Controls.Add(_painelErro);
            host.Controls.Add(_grid);
            host.SizeChanged += (s, e) => CentralizarSobreposicoes();

            Controls.Add(host);
            Controls.Add(rodape);

            // T-51: ordem de Tab explícita (UX-SPEC 5): grid -> "Tentar novamente" -> Emitir relatório (TabIndex 1,
            // definido em EmissaoRelatorioVisual) -> rodapé. Rótulos não recebem foco.
            _grid.AccessibleName = "Lista de vendas";
            _grid.AccessibleDescription = "Vendas registradas. Enter abre os detalhes da venda; F5 atualiza; F2 abre os detalhes do status.";
            _grid.TabIndex = 0;
            _painelErro.TabIndex = 1;
            _progresso.TabIndex = 2;
            _painelErro.AccessibleName = "Erro ao consultar";
            _btnTentar.AccessibleDescription = "Consulta novamente o banco de dados.";
            _btnTentar.TabIndex = 0;
            host.TabIndex = 1; // T-58: painel de filtros (quando anexado) usa TabIndex 0, antes da grid
            host.TabStop = false;
            rodape.TabIndex = 3;
            rodape.TabStop = false;
            ActiveControl = _grid; // foco inicial na grid (UX-SPEC 5, [A])

            KeyDown += (s, e) => TratarTecla(e);
            Shown += (s, e) => Recarregar();

            // T-44: duplo clique / Enter na linha abre F-3.
            _view.DoubleClick += (s, e) =>
            {
                var info = _view.CalcHitInfo(_grid.PointToClient(Control.MousePosition));
                if (info.InRow) AbrirDetalhe();
            };
            _grid.KeyDown += (s, e) => TratarTeclaGrid(e);
        }

        /// <summary>T-51: atalhos da janela. F5 atualiza; F2 abre os detalhes do status (RL10-01: caminho por teclado do BarStaticItem).</summary>
        internal void TratarTecla(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5) { e.Handled = true; Recarregar(); }
            else if (e.KeyCode == Keys.F2 && e.Modifiers == Keys.None) { e.Handled = true; _barraStatus?.AoClicar(); }
        }

        /// <summary>Enter na grid abre F-3 (T-44).</summary>
        internal void TratarTeclaGrid(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.Handled = true; AbrirDetalhe(); }
        }

        private static LabelControl CriarRotulo(DockStyle dock, HorzAlignment horizontal, string texto)
        {
            var l = new LabelControl { Dock = dock, AutoSizeMode = LabelAutoSizeMode.None, Text = texto };
            l.Appearance.TextOptions.HAlignment = horizontal;
            l.Appearance.TextOptions.VAlignment = VertAlignment.Center;
            return l;
        }

        /// <summary>T-44: presenter de F-3, definido pelo composition root (Program).</summary>
        public DetalheVendaPresenter DetalhePresenter { get; set; }

        private BarraStatusVisual _barraStatus;
        internal BarraStatusVisual BarraStatus => _barraStatus;

        /// <summary>T-45: anexa a barra de status F-6 e inicia a atualização (abertura + 15 s). Chamado pelo composition root (T-46).</summary>
        public void AnexarBarraStatus(BarraStatusPresenter presenter)
        {
            if (_barraStatus != null) return;
            _barraStatus = new BarraStatusVisual(this, presenter);
            presenter.Iniciar();
            FormClosed += (s, e) => _barraStatus.Dispose();
        }

        /// <summary>
        /// T-58: anexa o painel de filtros (F-2) acima da grid. <paramref name="textoVazio"/> escolhe o texto do estado
        /// vazio (com filtro ativo: "Nenhuma venda para os filtros informados."). TabIndex 0 = antes da grid.
        /// </summary>
        internal void AnexarPainelFiltros(Control painel, Func<string> textoVazio)
        {
            painel.TabIndex = 0;
            _host.TabIndex = 1;
            _rodape.TabIndex = 3;
            _textoVazio = textoVazio;
            Controls.Add(painel);
            painel.SendToBack(); // docking processa de trás p/ frente: filtros ficam no topo, acima do painel do relatório
        }

        /// <summary>T-44: abre F-3 (modal) para a linha focada; ao fechar, o foco volta à linha de origem.</summary>
        internal void AbrirDetalhe()
        {
            var linha = _view.GetFocusedRow() as LinhaConsultaVenda;
            if (linha == null || DetalhePresenter == null) return;
            int handle = _view.FocusedRowHandle;
            using (var detalhe = new FrmDetalheVenda(DetalhePresenter, linha))
            {
                detalhe.ShowDialog(this);
            }
            _grid.Focus();
            _view.FocusedRowHandle = handle;
        }

        internal GridControl Grid => _grid;
        internal SimpleButton BotaoTentar => _btnTentar;
        internal GridView View => _view;
        internal string TextoContador => _lblContador.Text;

        private void ConfigurarView()
        {
            _view.OptionsBehavior.Editable = false;          // somente leitura
            _view.OptionsBehavior.ReadOnly = true;
            _view.OptionsView.ShowAutoFilterRow = false;     // filtro automático DX desligado (UX-SPEC 2.1)
            _view.OptionsView.ShowGroupPanel = false;
            _view.OptionsCustomization.AllowFilter = false;
            _view.OptionsCustomization.AllowSort = true;     // ordenável
            _view.OptionsMenu.EnableColumnMenu = true;
            _view.OptionsView.ColumnAutoWidth = true;
            _view.OptionsSelection.EnableAppearanceFocusedCell = false;

            GridColumn Coluna(string campo, string titulo, int largura)
            {
                var c = _view.Columns.AddVisible(campo, titulo);
                c.Width = largura;
                c.OptionsColumn.AllowEdit = false;
                return c;
            }

            Coluna(nameof(LinhaConsultaVenda.VendaId), "Venda ID", 90);
            Coluna(nameof(LinhaConsultaVenda.ClienteId), "Cliente ID", 90);
            var valor = Coluna(nameof(LinhaConsultaVenda.ValorTotal), "Valor total", 100);
            valor.AppearanceCell.TextOptions.HAlignment = HorzAlignment.Far;
            valor.AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Far;

            var status = Coluna(nameof(LinhaConsultaVenda.StatusTexto), "Status", 110);
            var combo = new RepositoryItemImageComboBox { SmallImages = _icones, ReadOnly = true };
            combo.Items.Add(new ImageComboBoxItem("Quitada", "Quitada", (int)IconeStatus.Check));
            combo.Items.Add(new ImageComboBoxItem("Pendente", "Pendente", (int)IconeStatus.Relogio));
            combo.Items.Add(new ImageComboBoxItem("Cancelada", "Cancelada", (int)IconeStatus.X));
            combo.Items.Add(new ImageComboBoxItem("Cancelada*", "Cancelada*", (int)IconeStatus.X));
            _grid.RepositoryItems.Add(combo);
            status.ColumnEdit = combo;

            var recebida = Coluna(nameof(LinhaConsultaVenda.DataRecebimento), "Recebida em", 120);
            Coluna(nameof(LinhaConsultaVenda.DataQuitacao), "Quitada em", 120);
            var cancelada = Coluna(nameof(LinhaConsultaVenda.DataCancelamento), "Cancelada em", 120);
            cancelada.Visible = false; // oculta por padrão, disponível no column chooser

            // Ordenação padrão: Recebida em decrescente.
            recebida.SortOrder = ColumnSortOrder.Descending;

            _view.CustomColumnDisplayText += (s, e) =>
            {
                var linha = _view.GetRow(e.ListSourceRowIndex) as LinhaConsultaVenda;
                if (linha == null) return;
                switch (e.Column.FieldName)
                {
                    case nameof(LinhaConsultaVenda.ValorTotal): e.DisplayText = linha.ValorTotalTexto; break;
                    case nameof(LinhaConsultaVenda.DataRecebimento): e.DisplayText = linha.DataRecebimentoTexto; break;
                    case nameof(LinhaConsultaVenda.DataQuitacao): e.DisplayText = linha.DataQuitacaoTexto; break;
                    case nameof(LinhaConsultaVenda.DataCancelamento): e.DisplayText = linha.DataCancelamentoTexto; break;
                }
            };

            _grid.ToolTipController = new ToolTipController();
            _grid.ToolTipController.GetActiveObjectInfo += (s, e) =>
            {
                var info = _view.CalcHitInfo(e.ControlMousePosition);
                if (info.InRowCell && info.RowHandle >= 0)
                {
                    var linha = _view.GetRow(info.RowHandle) as LinhaConsultaVenda;
                    if (linha?.Tooltip != null)
                    {
                        e.Info = new ToolTipControlInfo(info.RowHandle + "|" + info.Column.FieldName, linha.Tooltip);
                    }
                }
            };
        }

        /// <summary>F5 / carga inicial. Não bloqueia a UI thread (regra 5).</summary>
        public async void Recarregar()
        {
            await RecarregarAsync();
        }

        internal async System.Threading.Tasks.Task RecarregarAsync()
        {
            if (_carregando) return;
            _carregando = true;
            MostrarCarregando();
            try
            {
                ModeloConsultaVendas modelo = await _presenter.CarregarAsync();
                Aplicar(modelo);
            }
            catch (Exception)
            {
                // Nunca expor stack trace ao usuário (UX-SPEC 4); detalhe técnico fica no log (T-46).
                MostrarErro();
            }
            finally
            {
                _carregando = false;
            }
        }

        internal EstadoConsulta Estado => _estado;
        // Control.Visible reflete a visibilidade efetiva (false sem o form exibido); usamos o valor solicitado.
        private bool _progressoOn, _erroOn;
        internal bool CarregandoVisivel => _progressoOn;
        internal bool ErroVisivel => _erroOn;
        private void Sobrepor(bool progresso, bool erro)
        {
            _progressoOn = progresso;
            _erroOn = erro;
            _progresso.Visible = progresso;
            _painelErro.Visible = erro;
        }
        internal string TextoErro => _lblErro.Text;
        internal string TextoAviso => _lblAviso.Text;
        internal string TextoCentralVazio => _estado == EstadoConsulta.Vazio ? (_textoVazio?.Invoke() ?? TextosConsulta.Vazio) : null;
        internal bool BotaoTentarNovamenteHabilitado => _btnTentar.Enabled;
        // PerformClick não dispara sem o form exibido (CanSelect); invoca o mesmo handler do Click.
        internal void ClicarTentarNovamente() => Recarregar();

        private void MostrarCarregando()
        {
            _estado = EstadoConsulta.Carregando;
            Sobrepor(true, false);
            _progresso.BringToFront();
            _grid.Enabled = false;
        }

        private void MostrarErro()
        {
            // Grid mantém dados anteriores, esmaecidos (Enabled=false), se existirem.
            _estado = EstadoConsulta.Erro;
            Sobrepor(false, true);
            _painelErro.BringToFront();
            _grid.Enabled = false;
            _lblContador.Text = "Erro ao carregar";
        }

        internal void Aplicar(ModeloConsultaVendas modelo)
        {
            _estado = modelo.Estado;
            Sobrepor(false, false);
            _grid.Enabled = true;
            _grid.DataSource = modelo.Linhas;
            _lblContador.Text = modelo.TextoContador;
            _lblAviso.Text = modelo.AvisoTruncado ?? string.Empty;
            _grid.Invalidate();
        }

        private void DesenharVazio(object sender, CustomDrawEventArgs e)
        {
            if (_estado != EstadoConsulta.Vazio) return;
            var fonte = e.Appearance.Font ?? Font;
            using (var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                var area = new RectangleF(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
                e.Graphics.DrawString(_textoVazio?.Invoke() ?? TextosConsulta.Vazio, fonte, SystemBrushes.GrayText, area, formato);
            }
            e.Handled = true;
        }
    }
}
