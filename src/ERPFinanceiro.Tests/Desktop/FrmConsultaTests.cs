using System;
using System.IO;
using System.Linq;
using System.Threading;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Infrastructure.Persistencia;
using DevExpress.Data;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// T-42: lógica de F-1 (presenter/linhas) contra Firebird embarcado real com massa
    /// equivalente ao seed T-06, mais configuração real de <c>GridControl</c>/<c>GridView</c>
    /// instanciados headless (thread STA, sem Show()). Aparência renderizada NÃO é coberta.
    /// </summary>
    public class FrmConsultaTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;

        public FrmConsultaTests()
        {
            _caminhoFdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "T42_FrmConsulta_" + Guid.NewGuid().ToString("N") + ".fdb");
            _configuracao = new Config { CaminhoFdb = _caminhoFdb };
            new DbInitializer(_configuracao).Inicializar();
        }

        public void Dispose()
        {
            try { if (File.Exists(_caminhoFdb)) File.Delete(_caminhoFdb); } catch (IOException) { }
        }

        private sealed class Config : IConfiguracaoBanco
        {
            public string CaminhoFdb { get; set; }
            public string Usuario { get; set; } = "sysdba";
            public string Senha { get; set; } = "masterkey";
        }

        private FbConnection Abrir()
        {
            return new FbConnection(new FbConnectionStringBuilder
            {
                Database = _configuracao.CaminhoFdb, UserID = _configuracao.Usuario, Password = _configuracao.Senha,
                ServerType = FbServerType.Embedded, Charset = "UTF8", Pooling = false
            }.ConnectionString);
        }

        private void SemearTresVendas()
        {
            using (var conn = Abrir())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repo = new VendaRepository(ctx);
                repo.Adicionar(Venda.CriarPorQuitacao("V-1001", "CLI-001", 500.50m,
                    new[] { new VendaItem("PROD-001", 2, 150.0000m), new VendaItem("PROD-002", 1, 200.5000m) },
                    new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 14, 30, 0, DateTimeKind.Utc)));
                repo.Adicionar(Venda.CriarPendente("V-1002", "CLI-002", 450.50m,
                    new[] { new VendaItem("PROD-003", 3, 100.0000m), new VendaItem("PROD-004", 2, 75.2500m) },
                    new DateTime(2026, 9, 5, 9, 15, 0, DateTimeKind.Utc)));
                repo.Adicionar(Venda.CriarCancelada("V-1003", "Venda nao localizada (D-08)", new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc)));
                ctx.SaveChanges();
            }
        }

        private ConsultaVendasPresenter CriarPresenter()
        {
            return new ConsultaVendasPresenter(() =>
            {
                using (var conn = Abrir())
                using (var ctx = new FinanceiroDbContext(conn))
                {
                    var repo = new VendaRepository(ctx);
                    return new ConsultaService(repo, repo).Listar(new FiltroVendas());
                }
            });
        }

        [Fact]
        public void CarregarAsync_ComSeed_ListaTresVendasComFormatosENulos()
        {
            SemearTresVendas();

            var modelo = CriarPresenter().CarregarAsync().GetAwaiter().GetResult();

            Assert.Equal("3 vendas", modelo.TextoContador);
            Assert.False(modelo.Truncado);
            Assert.Equal(new[] { "V-1003", "V-1002", "V-1001" }, modelo.Linhas.Select(l => l.VendaId).ToArray());

            var v1001 = modelo.Linhas[2];
            Assert.Equal("500,50", v1001.ValorTotalTexto);
            Assert.Equal("Quitada", v1001.StatusTexto);
            Assert.Equal(Formatadores.FormatarDataHoraLocal(new DateTime(2026, 9, 3, 14, 30, 0, DateTimeKind.Utc)), v1001.DataQuitacaoTexto);
            Assert.Null(v1001.Tooltip);

            var v1002 = modelo.Linhas[1];
            Assert.Equal("Pendente", v1002.StatusTexto);
            Assert.Equal("—", v1002.DataQuitacaoTexto);

            var v1003 = modelo.Linhas[0];
            Assert.Equal("—", v1003.ClienteId);
            Assert.Equal("—", v1003.ValorTotalTexto);
            Assert.Equal("Cancelada*", v1003.StatusTexto);
            Assert.Equal("—", v1003.DataQuitacaoTexto);
            Assert.Equal(LinhaConsultaVenda.TooltipCanceladaSemQuitacao, v1003.Tooltip);
        }

        [Fact]
        public void CarregarAsync_F5Recarrega_RefleteNovaVenda()
        {
            var presenter = CriarPresenter();
            Assert.Equal("0 vendas", presenter.CarregarAsync().GetAwaiter().GetResult().TextoContador);
            SemearTresVendas();
            Assert.Equal("3 vendas", presenter.CarregarAsync().GetAwaiter().GetResult().TextoContador);
        }

        [Fact]
        public void Contador_UmaVenda_Singular()
        {
            var modelo = ConsultaVendasPresenter.Montar(new ResultadoListagemVendas(
                new[] { new VendaListagemDto { VendaId = "V-1", ClienteId = "C", ValorTotal = 1m, DataRecebimento = DateTime.UtcNow } }, false));
            Assert.Equal("1 venda", modelo.TextoContador);
        }

        [Fact]
        public void Grid_ConfiguracaoReal_SomenteLeituraSemFiltroAutomaticoOrdenadoPorRecebimentoDesc()
        {
            SemearTresVendas();
            var modelo = CriarPresenter().CarregarAsync().GetAwaiter().GetResult();

            ExecutarSta(() =>
            {
                using (var frm = new FrmConsulta(CriarPresenter()))
                {
                    frm.Aplicar(modelo);
                    var view = frm.View;

                    Assert.False(view.OptionsBehavior.Editable);
                    Assert.False(view.OptionsView.ShowAutoFilterRow);
                    Assert.Equal(3, view.RowCount);

                    var visiveis = view.VisibleColumns.Select(c => c.FieldName).ToArray();
                    Assert.Equal(new[] { "VendaId", "ClienteId", "ValorTotal", "StatusTexto", "DataRecebimento", "DataQuitacao" }, visiveis);
                    Assert.False(view.Columns["DataCancelamento"].Visible); // oculta, disponível no column chooser
                    Assert.All(view.Columns.Cast<DevExpress.XtraGrid.Columns.GridColumn>(), c => Assert.False(c.OptionsColumn.AllowEdit));

                    var recebida = view.Columns["DataRecebimento"];
                    Assert.Equal(ColumnSortOrder.Descending, recebida.SortOrder);
                    Assert.Equal("V-1003", (string)view.GetRowCellValue(0, "VendaId"));
                    Assert.Equal("—", view.GetRowCellDisplayText(0, "ValorTotal"));
                    Assert.Equal("500,50", view.GetRowCellDisplayText(2, "ValorTotal"));
                    Assert.Equal("3 vendas", frm.TextoContador);
                }
            });
        }

        private static void ExecutarSta(Action acao)
        {
            Exception erro = null;
            var t = new Thread(() =>
            {
                try { acao(); } catch (Exception ex) { erro = ex; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            if (!t.Join(TimeSpan.FromSeconds(60)))
            {
                throw new TimeoutException("Teste de UI headless excedeu 60s.");
            }
            if (erro != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(erro).Throw();
            }
        }
    }
}
