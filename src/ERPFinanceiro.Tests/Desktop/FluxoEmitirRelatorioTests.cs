using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DevExpress.XtraEditors;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Infrastructure.Persistencia;
using ERPFinanceiro.Reports;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// T-50: fluxo F-5 (logica sem UI) contra Firebird embarcado real (seed) e o gerador real de PDF
    /// (T-49), com abridor de arquivo e apresentador de preview FALSOS. O ramo "com preview" e testado
    /// so com fake: nao existe preview real no FastReport Open Source adotado. WaitForm/dialogos/PDF
    /// abrindo no leitor real NAO sao cobertos (sem display): conferencia manual pendente.
    /// </summary>
    public class FluxoEmitirRelatorioTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;
        private readonly string _pasta;

        public FluxoEmitirRelatorioTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T50_Fluxo_" + Guid.NewGuid().ToString("N") + ".fdb");
            _configuracao = new Cfg { CaminhoFdb = _caminhoFdb };
            new DbInitializer(_configuracao).Inicializar();
            _pasta = Path.Combine(baseDir, "T50_pdfs_" + Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            try { if (File.Exists(_caminhoFdb)) File.Delete(_caminhoFdb); } catch { }
            try { if (Directory.Exists(_pasta)) Directory.Delete(_pasta, true); } catch { }
        }

        private sealed class Cfg : IConfiguracaoBanco
        {
            public string CaminhoFdb { get; set; }
            public string Usuario { get; set; } = "sysdba";
            public string Senha { get; set; } = "masterkey";
        }

        private sealed class AbridorFake : IAbridorArquivo
        {
            public List<string> Abertos = new List<string>();
            public Exception Falha;
            public void Abrir(string caminho) { if (Falha != null) throw Falha; Abertos.Add(caminho); }
        }

        private sealed class PreviewFake : IApresentadorPreviewRelatorio
        {
            public bool Disponivel { get; set; } = true;
            public int Chamadas;
            public ResultadoRelatorioVendas Recebido;
            public string FiltroRecebido;
            public void Exibir(ResultadoRelatorioVendas r, string filtro, DateTime em) { Chamadas++; Recebido = r; FiltroRecebido = filtro; }
        }

        private FirebirdSql.Data.FirebirdClient.FbConnection Abrir() => new FirebirdSql.Data.FirebirdClient.FbConnection(
            new FirebirdSql.Data.FirebirdClient.FbConnectionStringBuilder
            {
                Database = _configuracao.CaminhoFdb, UserID = _configuracao.Usuario, Password = _configuracao.Senha,
                ServerType = FirebirdSql.Data.FirebirdClient.FbServerType.Embedded, Charset = "UTF8", Pooling = false
            }.ConnectionString);

        private void Semear()
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
                repo.Adicionar(Venda.CriarCancelada("V-1003", "Venda nao localizada no sistema de origem (D-08)",
                    new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc)));
                ctx.SaveChanges();
            }
        }

        private Func<FiltroVendas, ResultadoRelatorioVendas> DadosReais()
        {
            return filtro =>
            {
                using (var conn = Abrir())
                using (var ctx = new FinanceiroDbContext(conn))
                {
                    var repo = new VendaRepository(ctx);
                    return new RelatorioDataSource(new ConsultaService(repo, repo)).Obter(filtro);
                }
            };
        }

        private FluxoEmitirRelatorio Criar(AbridorFake abridor, IApresentadorPreviewRelatorio preview = null, bool desativado = false,
            Action<ResultadoRelatorioVendas, string, DateTime, string> gerar = null, Func<string> pasta = null,
            Func<FiltroVendas, ResultadoRelatorioVendas> dados = null, Action<Exception> log = null)
        {
            var g = new GeradorRelatorioPdf();
            return new FluxoEmitirRelatorio(dados ?? DadosReais(), gerar ?? ((r, f, e, c) => g.Gerar(r, f, e, c)),
                pasta ?? (() => _pasta), abridor, preview ?? new ApresentadorPreviewIndisponivel(),
                () => new DateTime(2026, 9, 21, 14, 3, 0), desativado, log);
        }

        private static string[] Arquivos(string pasta) => Directory.Exists(pasta) ? Directory.GetFiles(pasta) : new string[0];

        [Fact]
        public void SemPreview_ComSeed_GeraPdfRealNaPastaTemporariaEAbre()
        {
            Semear();
            var abridor = new AbridorFake();
            var r = Criar(abridor).EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(TipoResultadoRelatorio.PdfAberto, r.Tipo);
            Assert.True(File.Exists(r.Caminho));
            Assert.StartsWith(_pasta, r.Caminho);
            Assert.True(new FileInfo(r.Caminho).Length > 0);
            using (var fs = File.OpenRead(r.Caminho))
            {
                var cab = new byte[4]; fs.Read(cab, 0, 4);
                Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(cab));
            }
            Assert.Equal(new[] { r.Caminho }, abridor.Abertos.ToArray());
            Assert.DoesNotContain(Arquivos(_pasta), f => f.EndsWith(".tmp"));
            Assert.False(r.ListaVazia);
            Assert.DoesNotContain("V-1", Path.GetFileName(r.Caminho)); // nome sem dados de negocio
        }

        [Fact]
        public void ListaVazia_GeraPdfComResultadoVazio()
        {
            var r = Criar(new AbridorFake()).EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();
            Assert.Equal(TipoResultadoRelatorio.PdfAberto, r.Tipo);
            Assert.True(r.ListaVazia);
            Assert.True(new FileInfo(r.Caminho).Length > 0);
        }

        [Fact]
        public void RelatorioUsaOFiltroCorrente_DescricaoEFiltroChegamAosDados()
        {
            FiltroVendas recebido = null;
            string descricao = null;
            var filtro = new FiltroVendas { ClienteId = "CLI-001", Status = StatusVenda.Quitada };
            var vazio = new ResultadoRelatorioVendas(new List<LinhaRelatorio>(), 0m, 0);
            var f = Criar(new AbridorFake(), dados: fi => { recebido = fi; return vazio; },
                gerar: (r, d, e, c) => { descricao = d; File.WriteAllText(c, "%PDF-fake"); });
            f.EmitirAsync(filtro, CancellationToken.None).GetAwaiter().GetResult();

            Assert.Same(filtro, recebido);
            Assert.Contains("Cliente: CLI-001", descricao);
            Assert.Contains("Status: Quitada", descricao);
        }

        [Fact]
        public void DescricaoFiltro_Padrao_MostraTodos()
        {
            Assert.Equal("Período: (todos) | Cliente: (todos) | Status: (todos)", DescricaoFiltro.De(new FiltroVendas()));
        }

        [Fact]
        public void ComPreview_Fake_ChamaApresentadorENaoGeraPdf()
        {
            Semear();
            var preview = new PreviewFake();
            var abridor = new AbridorFake();
            var r = Criar(abridor, preview).EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(TipoResultadoRelatorio.PreviewExibido, r.Tipo);
            Assert.Equal(1, preview.Chamadas);
            Assert.Equal(3, preview.Recebido.Linhas.Count);
            Assert.Empty(abridor.Abertos);
            Assert.Empty(Arquivos(_pasta));
        }

        [Fact]
        public void PreviewDisponivelMasForcadoSemPreview_UsaPdf()
        {
            Semear();
            var preview = new PreviewFake();
            var r = Criar(new AbridorFake(), preview, desativado: true).EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(TipoResultadoRelatorio.PdfAberto, r.Tipo);
            Assert.Equal(0, preview.Chamadas);
            Assert.True(File.Exists(r.Caminho));
        }

        [Fact]
        public void BuildAtual_ApresentadorIndisponivel_CaiEmPdf()
        {
            Assert.False(new ApresentadorPreviewIndisponivel().Disponivel);
            var r = Criar(new AbridorFake()).EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();
            Assert.Equal(TipoResultadoRelatorio.PdfAberto, r.Tipo);
        }

        [Fact]
        public void PreviewLancaExcecao_CaiEmPdf()
        {
            var preview = new PreviewThrow();
            var r = Criar(new AbridorFake(), preview).EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();
            Assert.Equal(TipoResultadoRelatorio.PdfAberto, r.Tipo);
        }

        private sealed class PreviewThrow : IApresentadorPreviewRelatorio
        {
            public bool Disponivel => true;
            public void Exibir(ResultadoRelatorioVendas r, string f, DateTime e) => throw new InvalidOperationException("x");
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("false", false)]
        [InlineData("lixo", false)]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("1", true)]
        public void ChaveDeConfiguracao_Interpretacao(string valor, bool esperado)
        {
            Assert.Equal(esperado, FluxoEmitirRelatorio.LerPreviewDesativado(valor));
            Assert.Equal("Relatorio:PreviewDesativado", TextosRelatorio.ChaveForcarSemPreview);
        }

        [Fact]
        public void Cancelamento_DuranteGeracao_NaoDeixaPdfNemTmp()
        {
            var cts = new CancellationTokenSource();
            var f = Criar(new AbridorFake(), gerar: (r, d, e, c) =>
            {
                File.WriteAllText(c, "%PDF-parcial");
                cts.Cancel();
            });
            var res = f.EmitirAsync(new FiltroVendas(), cts.Token).GetAwaiter().GetResult();

            Assert.Equal(TipoResultadoRelatorio.Cancelado, res.Tipo);
            Assert.Empty(Arquivos(_pasta));
        }

        [Fact]
        public void Cancelamento_AntesDeComecar_NaoConsultaNemGera()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            bool consultou = false;
            var f = Criar(new AbridorFake(), dados: fi => { consultou = true; return null; });
            var res = f.EmitirAsync(new FiltroVendas(), cts.Token).GetAwaiter().GetResult();
            Assert.Equal(TipoResultadoRelatorio.Cancelado, res.Tipo);
            Assert.False(consultou);
            Assert.Empty(Arquivos(_pasta));
        }

        [Fact]
        public void ThreadDeChamada_NaoEBloqueada_TrabalhoEmThreadDeFundo()
        {
            var liberar = new ManualResetEventSlim(false);
            var iniciou = new ManualResetEventSlim(false);
            int threadChamadora = Thread.CurrentThread.ManagedThreadId;
            int threadTrabalho = -1;
            var f = Criar(new AbridorFake(), dados: fi =>
            {
                threadTrabalho = Thread.CurrentThread.ManagedThreadId;
                iniciou.Set();
                liberar.Wait(TimeSpan.FromSeconds(30));
                return new ResultadoRelatorioVendas(new List<LinhaRelatorio>(), 0m, 0);
            }, gerar: (r, d, e, c) => File.WriteAllText(c, "%PDF-x"));

            var tarefa = f.EmitirAsync(new FiltroVendas(), CancellationToken.None);
            Assert.False(tarefa.IsCompleted); // retornou sem esperar o trabalho
            Assert.True(iniciou.Wait(TimeSpan.FromSeconds(30)));
            Assert.False(tarefa.IsCompleted); // trabalho em andamento enquanto o chamador segue livre
            liberar.Set();
            Assert.Equal(TipoResultadoRelatorio.PdfAberto, tarefa.GetAwaiter().GetResult().Tipo);
            Assert.NotEqual(threadChamadora, threadTrabalho);
        }

        [Fact]
        public void FalhaDeArquivo_PastaImpossivel_MensagemComCausaECaminhoSemStack()
        {
            string arquivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "T50_arquivo_" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(arquivo, "x");
            try
            {
                string pastaImpossivel = Path.Combine(arquivo, "sub"); // pai e um ARQUIVO
                Exception logado = null;
                var res = Criar(new AbridorFake(), pasta: () => pastaImpossivel, log: ex => logado = ex)
                    .EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();

                Assert.Equal(TipoResultadoRelatorio.ErroArquivo, res.Tipo);
                Assert.Contains("Causa:", res.Mensagem);
                Assert.Contains(pastaImpossivel, res.Mensagem);
                Assert.DoesNotContain(" at ", res.Mensagem);
                Assert.DoesNotContain("Exception", res.Mensagem);
                Assert.NotNull(logado);
            }
            finally { File.Delete(arquivo); }
        }

        [Fact]
        public void FalhaDeArquivo_SemPermissao_MensagemComCausaECaminho()
        {
            var res = Criar(new AbridorFake(), gerar: (r, d, e, c) => throw new UnauthorizedAccessException("negado"))
                .EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(TipoResultadoRelatorio.ErroArquivo, res.Tipo);
            Assert.Contains("sem permissão", res.Mensagem);
            Assert.Contains(_pasta, res.Mensagem);
            Assert.Empty(Arquivos(_pasta));
        }

        [Fact]
        public void FalhaDeArquivo_ArquivoBloqueado_IOException_MensagemComCausaECaminho()
        {
            var res = Criar(new AbridorFake(), gerar: (r, d, e, c) => throw new IOException("O arquivo esta sendo usado por outro processo.\r\nlinha2"))
                .EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(TipoResultadoRelatorio.ErroArquivo, res.Tipo);
            Assert.Contains("em uso", res.Mensagem);
            Assert.DoesNotContain("linha2", res.Mensagem);
            Assert.Contains(_pasta, res.Mensagem);
        }

        [Fact]
        public void FalhaDeDados_ErroDados_SemDetalheTecnicoNaMensagem()
        {
            var res = Criar(new AbridorFake(), dados: fi => throw new InvalidOperationException("SEGREDO conexao"))
                .EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(TipoResultadoRelatorio.ErroDados, res.Tipo);
            Assert.Equal(TextosRelatorio.ErroDados, res.Mensagem);
            Assert.DoesNotContain("SEGREDO", res.Mensagem);
        }

        [Fact]
        public void VisualizadorIndisponivel_PdfGeradoSemAbrir_MostraCaminho()
        {
            var abridor = new AbridorFake { Falha = new System.ComponentModel.Win32Exception("sem associacao") };
            var res = Criar(abridor).EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(TipoResultadoRelatorio.PdfGeradoSemAbrir, res.Tipo);
            Assert.True(res.Sucesso);
            Assert.True(File.Exists(res.Caminho));
            Assert.Contains(res.Caminho, res.Mensagem);
        }

        [Fact]
        public void Limpeza_ApagaAntigosPreservaRecentesEBloqueados()
        {
            Directory.CreateDirectory(_pasta);
            string antigo = Path.Combine(_pasta, "RelatorioVendas_antigo.pdf");
            string recente = Path.Combine(_pasta, "RelatorioVendas_recente.pdf");
            string bloqueado = Path.Combine(_pasta, "RelatorioVendas_bloqueado.pdf");
            string alheio = Path.Combine(_pasta, "outro.pdf");
            foreach (var f in new[] { antigo, recente, bloqueado, alheio }) File.WriteAllText(f, "%PDF");
            var velho = new DateTime(2026, 8, 1);
            File.SetLastWriteTime(antigo, velho);
            File.SetLastWriteTime(bloqueado, velho);
            File.SetLastWriteTime(alheio, velho);
            File.SetLastWriteTime(recente, new DateTime(2026, 9, 20));

            using (new FileStream(bloqueado, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var res = Criar(new AbridorFake()).EmitirAsync(new FiltroVendas(), CancellationToken.None).GetAwaiter().GetResult();
                Assert.True(res.Sucesso);
            }

            Assert.False(File.Exists(antigo));
            Assert.True(File.Exists(recente));
            Assert.True(File.Exists(bloqueado));
            Assert.True(File.Exists(alheio));
        }

        [Fact]
        public void CausaResumida_NaoVazaStack()
        {
            Exception e;
            try { throw new InvalidOperationException("falha\r\n   at Foo.Bar()"); } catch (Exception ex) { e = ex; }
            Assert.Equal("falha", FluxoEmitirRelatorio.CausaResumida(e));
        }

        [Fact]
        public void Visual_Headless_BotaoTextoAccessKeyECancelarDoAguarde()
        {
            ExecutarSta(() =>
            {
                using (var form = new XtraForm())
                {
                    bool consultado = false;
                    var fluxo = Criar(new AbridorFake(), dados: fi => { consultado = true; return null; });
                    string msg = null;
                    var visual = new EmissaoRelatorioVisual(form, fluxo, () => new FiltroVendas(), t => msg = t);
                    Assert.Equal("&Emitir relatório", visual.Botao.Text);
                    Assert.Equal("Emitir relatório", visual.Botao.AccessibleName);
                    Assert.True(visual.Botao.Enabled);
                    Assert.False(consultado);
                    Assert.Null(msg);
                }

                bool cancelou = false;
                using (var espera = new FrmAguardeRelatorio(() => cancelou = true))
                {
                    Assert.Equal("Gerando relatório...", espera.Progresso.Caption);
                    Assert.Equal("&Cancelar", espera.BotaoCancelar.Text);
                    Assert.Same(espera.BotaoCancelar, espera.CancelButton); // Esc cancela
                    espera.Cancelar();
                    Assert.True(cancelou);
                }
            });
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
    }
}
