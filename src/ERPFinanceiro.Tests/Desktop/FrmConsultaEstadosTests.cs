using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// T-43: estados de F-1 (vazio, carregando, erro, truncado). Lógica via presenter/modelo e
    /// FrmConsulta headless (STA, sem Show()). Renderização real (texto central, ProgressPanel)
    /// NÃO é coberta: pendente de conferência visual manual.
    /// </summary>
    public class FrmConsultaEstadosTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;

        public FrmConsultaEstadosTests()
        {
            _caminhoFdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "T43_Estados_" + Guid.NewGuid().ToString("N") + ".fdb");
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

        private FbConnection Abrir(string caminho = null)
        {
            return new FbConnection(new FbConnectionStringBuilder
            {
                Database = caminho ?? _configuracao.CaminhoFdb, UserID = _configuracao.Usuario, Password = _configuracao.Senha,
                ServerType = FbServerType.Embedded, Charset = "UTF8", Pooling = false
            }.ConnectionString);
        }

        private ConsultaVendasPresenter PresenterReal(string caminho = null)
        {
            return new ConsultaVendasPresenter(() =>
            {
                using (var conn = Abrir(caminho))
                using (var ctx = new FinanceiroDbContext(conn))
                {
                    var repo = new VendaRepository(ctx);
                    return new ConsultaService(repo, repo).Listar(new FiltroVendas());
                }
            });
        }

        [Fact]
        public void Textos_IguaisAoUxSpec4()
        {
            Assert.Equal("Nenhuma venda registrada ainda. As vendas aparecem aqui quando o ERP Vendas as enviar.", TextosConsulta.Vazio);
            Assert.Equal("Não foi possível consultar o banco.", TextosConsulta.Erro);
            Assert.Equal("Tentar novamente", TextosConsulta.BotaoTentarNovamente);
            Assert.Equal("Mostrando as 5.000 mais recentes; refine o filtro", TextosConsulta.AvisoTruncado);
        }

        [Fact]
        public void BancoVazio_Real_EstadoVazioSemAviso()
        {
            var modelo = PresenterReal().CarregarAsync().GetAwaiter().GetResult();
            Assert.Equal(EstadoConsulta.Vazio, modelo.Estado);
            Assert.Null(modelo.AvisoTruncado);

            ExecutarSta(() =>
            {
                using (var frm = new FrmConsulta(PresenterReal()))
                {
                    frm.Aplicar(modelo);
                    Assert.Equal(EstadoConsulta.Vazio, frm.Estado);
                    Assert.Equal(TextosConsulta.Vazio, frm.TextoCentralVazio);
                    Assert.Equal("0 vendas", frm.TextoContador);
                    Assert.False(frm.CarregandoVisivel);
                    Assert.False(frm.ErroVisivel);
                }
            });
        }

        [Fact]
        public void Carregando_ObservavelEnquantoTaskNaoTermina_UiThreadLivre()
        {
            var liberar = new ManualResetEventSlim(false);
            var presenter = new ConsultaVendasPresenter(() =>
            {
                liberar.Wait(TimeSpan.FromSeconds(30));
                return new ResultadoListagemVendas(new List<VendaListagemDto>(), false);
            });

            ExecutarSta(() =>
            {
                using (var frm = new FrmConsulta(presenter))
                {
                    var sw = Stopwatch.StartNew();
                    Task t = frm.RecarregarAsync();
                    // RecarregarAsync retornou ao chamador enquanto o carregador segue bloqueado.
                    Assert.True(sw.ElapsedMilliseconds < 5000);
                    Assert.False(t.IsCompleted);
                    Assert.Equal(EstadoConsulta.Carregando, frm.Estado);
                    Assert.True(frm.CarregandoVisivel);
                    Assert.False(frm.ErroVisivel);

                    liberar.Set();
                    Bombear(t);
                    Assert.Equal(EstadoConsulta.Vazio, frm.Estado);
                    Assert.False(frm.CarregandoVisivel);
                }
            });
        }

        [Fact]
        public void Erro_CarregadorLancaBancoIndisponivel_MostraMensagemSemStackETentarNovamenteRecarrega()
        {
            int chamadas = 0;
            var presenter = new ConsultaVendasPresenter(() =>
            {
                chamadas++;
                if (chamadas == 1) throw new BancoIndisponivelException("arquivo .fdb inacessivel C:\\segredo\\x.fdb", new IOException("detalhe tecnico"));
                return new ResultadoListagemVendas(new List<VendaListagemDto>(), false);
            });

            ExecutarSta(() =>
            {
                using (var frm = new FrmConsulta(presenter))
                {
                    Bombear(frm.RecarregarAsync());
                    Assert.Equal(EstadoConsulta.Erro, frm.Estado);
                    Assert.True(frm.ErroVisivel);
                    Assert.False(frm.CarregandoVisivel);
                    Assert.Equal(TextosConsulta.Erro, frm.TextoErro);
                    Assert.DoesNotContain("segredo", frm.TextoErro);
                    Assert.True(frm.BotaoTentarNovamenteHabilitado);

                    frm.ClicarTentarNovamente();
                    var limite = Stopwatch.StartNew();
                    while (frm.Estado != EstadoConsulta.Vazio && limite.Elapsed < TimeSpan.FromSeconds(30))
                    {
                        System.Windows.Forms.Application.DoEvents();
                        Thread.Sleep(10);
                    }
                    Assert.Equal(2, chamadas);
                    Assert.Equal(EstadoConsulta.Vazio, frm.Estado);
                    Assert.False(frm.ErroVisivel);
                }
            });
        }

        [Fact]
        public void Erro_FdbInacessivelReal_EstadoErro()
        {
            string inexistente = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pasta_inexistente_" + Guid.NewGuid().ToString("N"), "x.fdb");
            ExecutarSta(() =>
            {
                using (var frm = new FrmConsulta(PresenterReal(inexistente)))
                {
                    Bombear(frm.RecarregarAsync());
                    Assert.Equal(EstadoConsulta.Erro, frm.Estado);
                    Assert.Equal(TextosConsulta.Erro, frm.TextoErro);
                }
            });
        }

        [Fact]
        public void Truncado_5001LinhasReais_Mostra5000ComAviso()
        {
            var dataBase = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            using (var conn = Abrir())
            {
                conn.Open();
                using (var transacao = conn.BeginTransaction())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transacao;
                    cmd.CommandText =
                        "INSERT INTO FIN_VENDA (VENDA_ID, CLIENTE_ID, VALOR_TOTAL, STATUS, DATA_RECEBIMENTO, VERSAO) " +
                        "VALUES (@vendaId, @clienteId, @valorTotal, 0, @dataRecebimento, 0)";
                    cmd.Parameters.Add("@vendaId", FbDbType.VarChar, 50);
                    cmd.Parameters.Add("@clienteId", FbDbType.VarChar, 50);
                    cmd.Parameters.Add("@valorTotal", FbDbType.Decimal);
                    cmd.Parameters.Add("@dataRecebimento", FbDbType.TimeStamp);
                    cmd.Prepare();
                    for (int i = 0; i < 5001; i++)
                    {
                        cmd.Parameters["@vendaId"].Value = "V-MASSA-" + i.ToString("D5");
                        cmd.Parameters["@clienteId"].Value = "CLI-MASSA";
                        cmd.Parameters["@valorTotal"].Value = 10.00m;
                        cmd.Parameters["@dataRecebimento"].Value = dataBase.AddMinutes(i);
                        cmd.ExecuteNonQuery();
                    }
                    transacao.Commit();
                }
            }

            var modelo = PresenterReal().CarregarAsync().GetAwaiter().GetResult();
            Assert.True(modelo.Truncado);
            Assert.Equal(5000, modelo.Linhas.Count);
            Assert.Equal(EstadoConsulta.Dados, modelo.Estado);
            Assert.Equal("Mostrando as 5.000 mais recentes; refine o filtro", modelo.AvisoTruncado);

            ExecutarSta(() =>
            {
                using (var frm = new FrmConsulta(PresenterReal()))
                {
                    frm.Aplicar(modelo);
                    Assert.Equal(TextosConsulta.AvisoTruncado, frm.TextoAviso);
                    Assert.Equal("5000 vendas", frm.TextoContador);
                    Assert.Null(frm.TextoCentralVazio);
                }
            });
        }

        [Fact]
        public void ComDadosSemTruncar_AvisoLimpo()
        {
            var modelo = ConsultaVendasPresenter.Montar(new ResultadoListagemVendas(
                new[] { new VendaListagemDto { VendaId = "V-1", ClienteId = "C", ValorTotal = 1m, DataRecebimento = DateTime.UtcNow } }, false));
            ExecutarSta(() =>
            {
                using (var frm = new FrmConsulta(PresenterReal()))
                {
                    frm.Aplicar(modelo);
                    Assert.Equal(string.Empty, frm.TextoAviso);
                }
            });
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

        private static void ExecutarSta(Action acao)
        {
            Exception erro = null;
            var th = new Thread(() =>
            {
                try { acao(); } catch (Exception ex) { erro = ex; }
            });
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
            if (!th.Join(TimeSpan.FromSeconds(120)))
            {
                throw new TimeoutException("Teste de UI headless excedeu 120s.");
            }
            if (erro != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(erro).Throw();
            }
        }
    }
}
