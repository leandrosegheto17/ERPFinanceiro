using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Infrastructure.Persistencia;
using ERPFinanceiro.Reports;
using UglyToad.PdfPig;
using Xunit;

namespace ERPFinanceiro.Tests.Reports
{
    /// <summary>
    /// T-49: gera o PDF real (FastReport Open Source + PdfSimple, headless) com o seed real do
    /// Firebird embarcado e com lista vazia; confere cabecalho, colunas, total, nota de nulos e
    /// texto de lista vazia por TEXTO extraido (PdfPig, so teste). Aparencia visual (tipografia,
    /// alinhamento) NAO e verificada por estes testes: pendente de conferencia manual.
    /// </summary>
    public class GeradorRelatorioPdfTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;
        private readonly List<string> _temporarios = new List<string>();

        public GeradorRelatorioPdfTests()
        {
            _caminhoFdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "T49_Gerador_" + Guid.NewGuid().ToString("N") + ".fdb");
            _configuracao = new ConfiguracaoBancoTeste { CaminhoFdb = _caminhoFdb };
            new DbInitializer(_configuracao).Inicializar();
        }

        public void Dispose()
        {
            foreach (var f in _temporarios.Concat(new[] { _caminhoFdb }))
            {
                try { if (File.Exists(f)) File.Delete(f); } catch { }
            }
        }

        private sealed class ConfiguracaoBancoTeste : IConfiguracaoBanco
        {
            public string CaminhoFdb { get; set; }
            public string Usuario { get; set; } = "sysdba";
            public string Senha { get; set; } = "masterkey";
        }

        private FirebirdSql.Data.FirebirdClient.FbConnection AbrirConexao()
        {
            var csb = new FirebirdSql.Data.FirebirdClient.FbConnectionStringBuilder
            {
                Database = _configuracao.CaminhoFdb,
                UserID = _configuracao.Usuario,
                Password = _configuracao.Senha,
                ServerType = FirebirdSql.Data.FirebirdClient.FbServerType.Embedded,
                Charset = "UTF8",
                Pooling = false
            };
            return new FirebirdSql.Data.FirebirdClient.FbConnection(csb.ConnectionString);
        }

        private string NovoCaminhoPdf()
        {
            // ERP_T49_SAIDA (opcional): pasta onde os PDFs de exemplo ficam preservados para conferencia visual manual.
            string pasta = Environment.GetEnvironmentVariable("ERP_T49_SAIDA");
            if (!string.IsNullOrEmpty(pasta) && Directory.Exists(pasta))
                return Path.Combine(pasta, "T49_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".pdf");
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "T49_" + Guid.NewGuid().ToString("N") + ".pdf");
            _temporarios.Add(p);
            return p;
        }

        // O PDF do FastReport Open Source (PdfSimple) e RASTER: cada pagina e uma imagem JPEG, sem texto
        // extraivel. Por isso: (1) o PDF e validado estruturalmente com PdfPig (abre, numero de paginas,
        // cada pagina contem imagem); (2) o CONTEUDO e verificado no HTML exportado da MESMA renderizacao
        // (mesmo layout/dados/Prepare), com texto real.
        private static string TextoDoRelatorio(string pdf, ResultadoRelatorioVendas r, string filtro, DateTime em, out int paginas)
        {
            using (var doc = PdfDocument.Open(pdf))
            {
                paginas = doc.NumberOfPages;
                foreach (var pagina in doc.GetPages())
                    Assert.True(pagina.GetImages().Any(), "pagina do PDF raster deve conter imagem");
            }
            using (var ms = new MemoryStream())
            {
                new GeradorRelatorioPdf().GerarHtml(r, filtro, em, ms);
                string html = Encoding.UTF8.GetString(ms.ToArray());
                html = Regex.Replace(html, "<style.*?</style>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                return System.Net.WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
            }
        }

        private static string Normalizar(string s) => Regex.Replace(s, @"\s+", " ");

        private ResultadoRelatorioVendas ObterComSeed()
        {
            var v1001 = Venda.CriarPorQuitacao("V-1001", "CLI-001", 500.50m,
                new[] { new VendaItem("PROD-001", 2, 150.0000m), new VendaItem("PROD-002", 1, 200.5000m) },
                new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 14, 30, 0, DateTimeKind.Utc));
            var v1002 = Venda.CriarPendente("V-1002", "CLI-002", 450.50m,
                new[] { new VendaItem("PROD-003", 3, 100.0000m), new VendaItem("PROD-004", 2, 75.2500m) },
                new DateTime(2026, 9, 5, 9, 15, 0, DateTimeKind.Utc));
            var v1003 = Venda.CriarCancelada("V-1003", "Venda nao localizada no sistema de origem (D-08)",
                new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc));

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repo = new VendaRepository(ctx);
                repo.Adicionar(v1001);
                repo.Adicionar(v1002);
                repo.Adicionar(v1003);
                ctx.SaveChanges();
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repo = new VendaRepository(ctx);
                return new RelatorioDataSource(new ConsultaService(repo, repo)).Obter(new FiltroVendas());
            }
        }

        [Fact]
        public void Gerar_ComSeed_PdfValidoComCabecalhoColunasTotalENotaDeNulos()
        {
            var resultado = ObterComSeed();
            string pdf = NovoCaminhoPdf();

            new GeradorRelatorioPdf().Gerar(resultado, "01/09/2026 a 21/09/2026 | Cliente: (todos) | Status: (todos)",
                new DateTime(2026, 9, 21, 14, 3, 0), pdf);

            Assert.True(File.Exists(pdf));
            Assert.True(new FileInfo(pdf).Length > 0);
            using (var fs = File.OpenRead(pdf))
            {
                var cab = new byte[4];
                fs.Read(cab, 0, 4);
                Assert.Equal("%PDF", Encoding.ASCII.GetString(cab));
            }

            int paginas;
            string texto = Normalizar(TextoDoRelatorio(pdf, resultado, "01/09/2026 a 21/09/2026 | Cliente: (todos) | Status: (todos)", new DateTime(2026, 9, 21, 14, 3, 0), out paginas));
            Assert.Equal(1, paginas);
            Assert.Contains("RELATÓRIO FINANCEIRO DE VENDAS", texto);
            Assert.Contains("Emitido em 21/09/2026 14:03", texto);
            Assert.Contains("Filtro: 01/09/2026 a 21/09/2026 | Cliente: (todos) | Status: (todos)", texto);
            foreach (var col in new[] { "VendaId", "ClienteId", "Valor", "Status", "Recebida", "Quitada", "Cancelada" })
                Assert.Contains(col, texto);
            Assert.Contains("V-1001", texto);
            Assert.Contains("V-1002", texto);
            Assert.Contains("V-1003", texto);
            Assert.Contains("500,50", texto);
            Assert.Contains("450,50", texto);
            Assert.Contains("Total listado: R$ 951,00", texto);
            Assert.Contains("Nota: 1 venda com valor nulo", texto);
            Assert.DoesNotContain("Nenhuma venda no período", texto);
        }

        [Fact]
        public void Gerar_ListaVazia_MostraMensagemETotalZero_SemNotaDeNulos()
        {
            var vazio = new ResultadoRelatorioVendas(new List<LinhaRelatorio>(), 0m, 0);
            string pdf = NovoCaminhoPdf();

            new GeradorRelatorioPdf().Gerar(vazio, "(sem filtro)", new DateTime(2026, 9, 21, 14, 3, 0), pdf);

            using (var fs = File.OpenRead(pdf))
            {
                var cab = new byte[4];
                fs.Read(cab, 0, 4);
                Assert.Equal("%PDF", Encoding.ASCII.GetString(cab));
            }
            int paginas;
            string texto = Normalizar(TextoDoRelatorio(pdf, vazio, "(sem filtro)", new DateTime(2026, 9, 21, 14, 3, 0), out paginas));
            Assert.Equal(1, paginas);
            Assert.Contains("Nenhuma venda no período", texto);
            Assert.Contains("Total listado: R$ 0,00", texto);
            Assert.DoesNotContain("Nota:", texto);
        }

        [Fact]
        public void Gerar_MuitasLinhas_QuebraEmVariasPaginasComTotalNoFim()
        {
            var linhas = new List<LinhaRelatorio>();
            for (int i = 0; i < 200; i++)
                linhas.Add(new LinhaRelatorio
                {
                    VendaId = "V-" + i.ToString("D4"),
                    ClienteId = "CLI-1",
                    ValorTotal = 1m,
                    Status = StatusVenda.Pendente,
                    DataRecebimento = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc)
                });
            string pdf = NovoCaminhoPdf();

            var res = new ResultadoRelatorioVendas(linhas, 200m, 0);
            var em = new DateTime(2026, 9, 21, 14, 3, 0);
            new GeradorRelatorioPdf().Gerar(res, "x", em, pdf);

            int paginas;
            string texto = Normalizar(TextoDoRelatorio(pdf, res, "x", em, out paginas));
            Assert.True(paginas > 1, "esperado mais de 1 pagina, obtido " + paginas);
            Assert.Contains("V-0199", texto);
            Assert.Contains("Total listado: R$ 200,00", texto);
            Assert.Contains("Página 1 de " + paginas, texto);
        }

        [Fact]
        public void FormatacaoRelatorio_IgualAFormatadoresDoDesktop()
        {
            foreach (var v in new[] { 0m, 951m, 1250.5m, 12500m, 0.005m, 1234567.891m })
                Assert.Equal(Formatadores.FormatarMoeda(v), FormatacaoRelatorio.Moeda(v));

            var utc = new DateTime(2026, 9, 21, 17, 3, 0, DateTimeKind.Utc);
            Assert.Equal(Formatadores.FormatarDataHoraLocal(utc), FormatacaoRelatorio.DataHoraLocal(utc));
            Assert.Equal(Formatadores.FormatarDataHoraLocal((DateTime?)null), FormatacaoRelatorio.DataHoraLocal((DateTime?)null));

            foreach (StatusVenda s in Enum.GetValues(typeof(StatusVenda)))
                Assert.Equal(Formatadores.FormatarStatus(s).Texto, FormatacaoRelatorio.Status(s));
        }

        [Fact]
        public void FastReportOpenSource_NaoTemPreviewControlWinForms_ConfirmaLicencasMd()
        {
            // Confirma por reflexao (T-49) o que docs/licencas.md so afirmava por documentacao: o pacote
            // Open Source 2023.3.13 nao traz PreviewControl WinForms (so o recurso de string homonimo).
            // Se este teste quebrar ao atualizar o pacote, revisar ADR-008 (fluxo de preview, T-50).
            Assert.Null(typeof(FastReport.Report).Assembly.GetType("FastReport.Preview.PreviewControl"));
            Assert.Null(typeof(FastReport.Report).Assembly.GetType("FastReport.PreviewControl"));
        }

        [Fact]
        public void Layout_NaoTemCorLiteralForaDoBlocoDeEstilos()
        {
            string frx;
            using (var s = typeof(GeradorRelatorioPdf).Assembly.GetManifestResourceStream(GeradorRelatorioPdf.RecursoLayout))
            using (var r = new StreamReader(s, Encoding.UTF8))
                frx = r.ReadToEnd();

            int ini = frx.IndexOf("<Styles>", StringComparison.Ordinal);
            int fim = frx.IndexOf("</Styles>", StringComparison.Ordinal);
            Assert.True(ini >= 0 && fim > ini);
            string fora = Regex.Replace(frx.Remove(ini, fim - ini), "<!--.*?-->", "", RegexOptions.Singleline);
            Assert.DoesNotMatch(@"(Color|Fill\.|TextFill|Border\.Color)", fora);
        }
    }
}
