using System;
using System.IO;
using System.Linq;
using System.Threading;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// T-44: lógica de F-3 contra Firebird embarcado real (massa equivalente ao seed T-06) e
    /// construção headless do form (STA, sem Show/ShowDialog). Aparência/foco/Esc reais NÃO cobertos.
    /// </summary>
    public class FrmDetalheVendaTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;

        public FrmDetalheVendaTests()
        {
            _caminhoFdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "T44_Detalhe_" + Guid.NewGuid().ToString("N") + ".fdb");
            _configuracao = new Config { CaminhoFdb = _caminhoFdb };
            new DbInitializer(_configuracao).Inicializar();
            Semear();
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

        private void Semear()
        {
            using (var conn = Abrir())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repo = new VendaRepository(ctx);
                repo.Adicionar(Venda.CriarPorQuitacao("V-1001", "CLI-001", 500.50m,
                    new[] { new VendaItem("PROD-001", 2, 150.0000m), new VendaItem("PROD-002", 1, 200.5000m) },
                    new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 14, 30, 0, DateTimeKind.Utc)));
                repo.Adicionar(Venda.CriarCancelada("V-1003", "Venda nao localizada (D-08)", new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc)));
                ctx.SaveChanges();
            }
        }

        private DetalheVendaPresenter CriarPresenter()
        {
            return new DetalheVendaPresenter(id =>
            {
                using (var conn = Abrir())
                using (var ctx = new FinanceiroDbContext(conn))
                {
                    var repo = new VendaRepository(ctx);
                    return new ConsultaService(repo, repo).ObterDetalhe(id);
                }
            });
        }

        private LinhaConsultaVenda Linha(string vendaId)
        {
            using (var conn = Abrir())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repo = new VendaRepository(ctx);
                var r = new ConsultaService(repo, repo).Listar(new FiltroVendas());
                return new LinhaConsultaVenda(r.Itens.Single(i => i.VendaId == vendaId));
            }
        }

        [Fact]
        public void Presenter_V1001_ItensSubtotaisETotalConferemComValorDaVenda()
        {
            var m = CriarPresenter().CarregarAsync("V-1001").GetAwaiter().GetResult();

            Assert.False(m.SemItens);
            Assert.Equal(new[] { "300,00", "200,50" }, m.Itens.Select(i => i.Subtotal).ToArray());
            Assert.Equal("150,0000", m.Itens[0].PrecoUnitario);
            Assert.Equal("Total itens: 500,50", m.TotalItensTexto);
            Assert.Equal("500,50", m.Cabecalho.Valor); // soma dos subtotais = valor da venda
            Assert.Equal("Venda V-1001", m.Cabecalho.Titulo);
            Assert.Equal("CLI-001", m.Cabecalho.Cliente);
            Assert.Equal("Quitada", m.Cabecalho.Status);
            Assert.Equal("—", m.Cabecalho.CancelamentoMotivo);
        }

        [Fact]
        public void Presenter_V1003_CanceladaSemQuitacao_SemItensSemErroComTextoExplicativo()
        {
            var m = CriarPresenter().CarregarAsync("V-1003").GetAwaiter().GetResult();

            Assert.True(m.SemItens);
            Assert.Equal("Total itens: 0,00", m.TotalItensTexto);
            Assert.Equal("Sem itens registrados (venda cancelada sem quitação prévia)", ModeloDetalheVenda.TextoSemItens);
            Assert.Equal("Cancelada*", m.Cabecalho.Status);
            Assert.Equal("—", m.Cabecalho.Cliente);
            Assert.Equal("—", m.Cabecalho.Valor);
            Assert.EndsWith("/ Venda nao localizada (D-08)", m.Cabecalho.CancelamentoMotivo);
        }

        [Fact]
        public void Presenter_VendaInexistente_PropagaExcecao()
        {
            Assert.ThrowsAny<Exception>(() => CriarPresenter().CarregarAsync("V-9999").GetAwaiter().GetResult());
        }

        [Fact]
        public void Form_ComItens_MostraCabecalhoGridSomenteLeituraSemAbaHistorico()
        {
            var linha = Linha("V-1001");
            var modelo = CriarPresenter().CarregarAsync("V-1001").GetAwaiter().GetResult();
            ExecutarSta(() =>
            {
                using (var frm = new FrmDetalheVenda(CriarPresenter(), linha))
                {
                    Assert.Equal("Venda V-1001", frm.TextoTitulo); // cabeçalho imediato da linha
                    Assert.Contains("Cliente: CLI-001", frm.TextoLinha1);
                    frm.Aplicar(modelo);
                    Assert.Equal(2, frm.View.RowCount);
                    Assert.False(frm.View.OptionsBehavior.Editable);
                    Assert.Equal("300,00", frm.View.GetRowCellDisplayText(0, "Subtotal"));
                    Assert.Equal("Total itens: 500,50", frm.TextoTotal);
                    Assert.Null(frm.TextoAviso);
                    Assert.False(frm.TentarNovamenteVisivel);
                    Assert.Equal(1, frm.QuantidadeAbas); // só Itens; Histórico é Tier C
                    Assert.Same(frm.BotaoFechar, frm.CancelButton); // Esc fecha
                }
            });
        }

        [Fact]
        public void Form_CanceladaSemQuitacao_MostraTextoExplicativo()
        {
            var linha = Linha("V-1003");
            var modelo = CriarPresenter().CarregarAsync("V-1003").GetAwaiter().GetResult();
            ExecutarSta(() =>
            {
                using (var frm = new FrmDetalheVenda(CriarPresenter(), linha))
                {
                    frm.Aplicar(modelo);
                    Assert.Equal(0, frm.View.RowCount);
                    Assert.Equal(ModeloDetalheVenda.TextoSemItens, frm.TextoAviso);
                    Assert.False(frm.TentarNovamenteVisivel);
                }
            });
        }

        [Fact]
        public void Form_FalhaNosItens_MostraAvisoETentarNovamenteSemFechar()
        {
            var linha = Linha("V-1001");
            ExecutarSta(() =>
            {
                using (var frm = new FrmDetalheVenda(CriarPresenter(), linha))
                {
                    frm.MostrarErro();
                    Assert.Equal(DetalheVendaPresenter.TextoErro, frm.TextoAviso);
                    Assert.True(frm.TentarNovamenteVisivel);
                    Assert.False(frm.IsDisposed);
                    Assert.Equal("Venda V-1001", frm.TextoTitulo);
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
