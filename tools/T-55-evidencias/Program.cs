using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Autofac;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Entities;
using UglyToad.PdfPig;

namespace T55Evidencias
{
    /// <summary>
    /// Harness T-55. Usa o codigo REAL de UI/relatorio (FrmConsulta, FrmDetalheVenda, barra de status,
    /// FluxoEmitirRelatorio + GeradorRelatorioPdf + ConsultaService) e captura a janela real por
    /// CopyFromScreen. Argumento 1: pasta de saida. Argumento 2 (opcional):
    ///  - (omitido) modo "banco": Firebird embarcado real, criado por 01-schema.sql + 02-seed.sql
    ///    (Executar.ps1), API real (ApiHost) e health real;
    ///  - "memoria": mesmas 3 vendas do seed (mesmos valores de database/02-seed.sql, via fabricas de dominio)
    ///    em repositorio em memoria; health e estado da API sao STUBS (ver docs/evidencias/README.md).
    /// </summary>
    internal static class Program
    {
        private static string _saida;

        [STAThread]
        private static int Main(string[] args)
        {
            _saida = args.Length > 0 ? args[0] : "evidencias";
            bool memoria = args.Length > 1 && args[1] == "memoria";
            Directory.CreateDirectory(_saida);
            Config("AplicarAcessibilidadeDpi");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Config("AplicarSkin");
            Console.WriteLine("Modo: " + (memoria ? "MEMORIA (seed equivalente, sem Firebird)" : "BANCO (Firebird + seed real)"));

            IContainer container = null;
            ApiHost host = null;
            Func<ConsultaService> novoServico;
            IHealthService health;
            IFonteEstadoApi fonte;
            string fdb;
            int porta = 5055;

            if (memoria)
            {
                var repo = new RepositorioMemoria();
                novoServico = () => new ConsultaService(repo, repo);
                health = new HealthStub();
                fonte = new FonteApiStub(porta);
                fdb = "(seed em memoria - stub)";
            }
            else
            {
                container = CompositionRoot.Construir();
                host = new ApiHost(container);
                porta = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Api:Porta"]);
                host.Start(porta);
                Console.WriteLine("API: " + host.Estado);
                novoServico = () => container.BeginLifetimeScope().Resolve<ConsultaService>();
                health = container.Resolve<IHealthService>();
                fonte = new FonteEstadoApiHost(host);
                fdb = container.Resolve<IConfiguracaoBanco>().CaminhoFdb;
            }

            int falhas = 0;
            var detalhePresenter = new DetalheVendaPresenter(id => novoServico().ObterDetalhe(id));
            var consulta = new ConsultaVendasPresenter(() => novoServico().Listar(new FiltroVendas()));

            string pastaPdf = Path.Combine(_saida, "_tmp_pdf");
            var gerador = new ERPFinanceiro.Reports.GeradorRelatorioPdf();
            var fluxo = new FluxoEmitirRelatorio(
                obterDados: f => new ERPFinanceiro.Reports.RelatorioDataSource(novoServico()).Obter(f),
                gerarPdf: (d, f, em, caminho) => gerador.Gerar(d, f, em, caminho),
                pastaTemporaria: () => pastaPdf,
                abridor: new NaoAbre(),
                preview: new ApresentadorPreviewIndisponivel(),
                agoraLocal: () => DateTime.Now,
                previewDesativado: false);

            var janela = new FrmConsulta(consulta) { DetalhePresenter = detalhePresenter, StartPosition = FormStartPosition.Manual, Location = new Point(20, 20) };
            var barra = new BarraStatusPresenter(fonte, health, porta, fdb, new AreaTransferenciaWinForms(), new TemporizadorWinForms());
            janela.AnexarBarraStatus(barra);
            new EmissaoRelatorioVisual(janela, fluxo, () => new FiltroVendas(), t => { });
            janela.Show();
            janela.Recarregar();
            Esperar(() => (EstadoConsulta)Prop(janela, "Estado") == EstadoConsulta.Dados
                          && !barra.Banco.Texto.Contains("verificando"), 20000);
            Console.WriteLine("Estado lista: " + Prop(janela, "Estado") + " | " + barra.Api.Texto + " | " + barra.Banco.Texto);
            Bitmap lista = Capturar(janela, Path.Combine(_saida, "01-lista.png"));

            // Indicador: recorte real da barra de status (rodape da janela) + dialogo de detalhes.
            int alt = Math.Min(40, lista.Height);
            using (var crop = lista.Clone(new Rectangle(0, lista.Height - alt, lista.Width, alt), lista.PixelFormat))
                crop.Save(Path.Combine(_saida, "04-indicador-barra-status.png"), ImageFormat.Png);
            using (var det = new FrmDetalhesStatus(barra) { StartPosition = FormStartPosition.Manual, Location = new Point(80, 80) })
            {
                det.Show(); Bombear(600);
                Capturar(det, Path.Combine(_saida, "05-indicador-detalhes-status.png"));
            }

            // Detalhe F-3 da venda V-1001 (Quitada, 2 itens do seed).
            VendaListagemDto dto = novoServico().Listar(new FiltroVendas()).Itens.First(v => v.VendaId == "V-1001");
            using (var det = new FrmDetalheVenda(detalhePresenter, new LinhaConsultaVenda(dto)) { StartPosition = FormStartPosition.Manual, Location = new Point(60, 60) })
            {
                det.Show(); Bombear(2500);
                Capturar(det, Path.Combine(_saida, "02-detalhe.png"));
            }

            // Relatorio real (mesmo fluxo do botao "Emitir relatorio", filtro sem restricao).
            var res = fluxo.EmitirAsync(new FiltroVendas(), CancellationToken.None).Result;
            Console.WriteLine("Relatorio: " + res.Tipo + " | " + res.Caminho + " | " + res.Mensagem);
            if (res.Sucesso && File.Exists(res.Caminho))
            {
                string destino = Path.Combine(_saida, "relatorio-exemplo.pdf");
                File.Copy(res.Caminho, destino, true);
                VerificarPdf(destino);
            }
            else { Console.Error.WriteLine("FALHA ao gerar PDF"); falhas++; }
            try { Directory.Delete(pastaPdf, true); } catch { }

            janela.Close();
            host?.Stop();
            host?.Dispose();
            container?.Dispose();

            // Erro F-1: MESMO formulario/presenter reais, com carregador que falha (banco indisponivel simulado).
            var erroPresenter = new ConsultaVendasPresenter(() => { throw new InvalidOperationException("Banco indisponivel (simulado pelo harness T-55)"); });
            using (var fErro = new FrmConsulta(erroPresenter) { StartPosition = FormStartPosition.Manual, Location = new Point(20, 20) })
            {
                fErro.Show(); fErro.Recarregar();
                Esperar(() => (EstadoConsulta)Prop(fErro, "Estado") == EstadoConsulta.Erro, 10000);
                Bombear(500);
                Console.WriteLine("Estado erro: " + Prop(fErro, "Estado"));
                Capturar(fErro, Path.Combine(_saida, "03-erro.png"));
            }
            return falhas;
        }

        // ConfiguracaoVisual e internal no Desktop: chamada por reflexao (sem alterar codigo de producao).
        private static void Config(string metodo) =>
            typeof(FrmConsulta).Assembly.GetType("ERPFinanceiro.Desktop.ConfiguracaoVisual")
                .GetMethod(metodo, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, null);

        private sealed class NaoAbre : IAbridorArquivo { public void Abrir(string caminho) { } }

        private sealed class HealthStub : IHealthService
        {
            public ResultadoHealth ObterStatus() => ResultadoHealth.ComSucesso("ok (stub do harness)");
        }

        private sealed class FonteApiStub : IFonteEstadoApi
        {
            private readonly int _porta;
            public FonteApiStub(int porta) { _porta = porta; }
            public EstadoApiHost Estado => EstadoApiHost.Ativa;
            public Exception UltimoErro => null;
            public string EnderecoBase => "http://localhost:" + _porta + "/";
        }

        /// <summary>As 3 vendas de database/02-seed.sql (mesmos valores), como em GeradorRelatorioPdfTests.ObterComSeed.</summary>
        private sealed class RepositorioMemoria : IVendaRepository, IVendaConsultaLeitura
        {
            private readonly List<Venda> _vendas = new List<Venda>
            {
                Venda.CriarPorQuitacao("V-1001", "CLI-001", 500.50m,
                    new[] { new VendaItem("PROD-001", 2, 150.0000m), new VendaItem("PROD-002", 1, 200.5000m) },
                    new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 14, 30, 0, DateTimeKind.Utc)),
                Venda.CriarPendente("V-1002", "CLI-002", 450.50m,
                    new[] { new VendaItem("PROD-003", 3, 100.0000m), new VendaItem("PROD-004", 2, 75.2500m) },
                    new DateTime(2026, 9, 5, 9, 15, 0, DateTimeKind.Utc)),
                Venda.CriarCancelada("V-1003", "Venda nao localizada no sistema de origem (D-08)",
                    new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc)),
            };
            public Venda ObterPorVendaId(string vendaId) => _vendas.FirstOrDefault(v => v.VendaId == vendaId);
            public void Adicionar(Venda venda) { _vendas.Add(venda); }
            public IReadOnlyList<Venda> ListarMaisRecentes(int max) => _vendas.OrderByDescending(v => v.DataRecebimento).Take(max).ToList();
            public IReadOnlyList<Venda> ListarTodas() => _vendas.OrderByDescending(v => v.DataRecebimento).ToList();
        }

        private static object Prop(object o, string nome) =>
            o.GetType().GetProperty(nome, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(o, null);

        private static void Bombear(int ms)
        {
            var fim = DateTime.UtcNow.AddMilliseconds(ms);
            while (DateTime.UtcNow < fim) { Application.DoEvents(); Thread.Sleep(20); }
        }

        private static void Esperar(Func<bool> cond, int ms)
        {
            var fim = DateTime.UtcNow.AddMilliseconds(ms);
            while (DateTime.UtcNow < fim && !cond()) { Application.DoEvents(); Thread.Sleep(30); }
            Bombear(800);
        }

        private static Bitmap Capturar(Form f, string arquivo)
        {
            f.Activate(); f.BringToFront(); Bombear(400);
            var r = f.Bounds;
            var bmp = new Bitmap(r.Width, r.Height);
            using (var g = Graphics.FromImage(bmp)) g.CopyFromScreen(r.Location, Point.Empty, r.Size);
            bmp.Save(arquivo, ImageFormat.Png);
            Console.WriteLine("Print: " + arquivo + " " + r.Width + "x" + r.Height);
            return bmp;
        }

        private static void VerificarPdf(string caminho)
        {
            byte[] b = File.ReadAllBytes(caminho);
            string cab = System.Text.Encoding.ASCII.GetString(b, 0, 5);
            using (var doc = PdfDocument.Open(caminho))
                Console.WriteLine("PDF: cabecalho=" + cab + " bytes=" + b.Length + " paginas=" + doc.NumberOfPages);
        }
    }
}
