using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ERPFinanceiro.Reports;
using Xunit;

namespace ERPFinanceiro.Tests.Reports
{
    /// <summary>T-49: renderiza o .frx com seed/lista vazia e exporta PDF (FastReport Open Source, sem GUI).</summary>
    public class RelatorioListagemTests
    {
        private const string Filtro = "Filtro: 01/09/2026 a 21/09/2026 | Cliente: (todos) | Status: (todos)";

        private static RelatorioDados Seed()
        {
            var linhas = new List<LinhaRelatorio>
            {
                new LinhaRelatorio { VendaId = "V-1002", ClienteId = "CLI-002", ValorTotal = 450.50m, Status = "Pendente", DataRecebimento = new DateTime(2026, 9, 5) },
                new LinhaRelatorio { VendaId = "V-1003", ClienteId = "—", ValorTotal = 0m, ValorNulo = true, Status = "Cancelada", DataRecebimento = new DateTime(2026, 9, 3), DataCancelamento = new DateTime(2026, 9, 4) },
                new LinhaRelatorio { VendaId = "V-1001", ClienteId = "CLI-001", ValorTotal = 500.50m, Status = "Quitada", DataRecebimento = new DateTime(2026, 9, 1), DataQuitacao = new DateTime(2026, 9, 2) }
            };
            return new RelatorioDados { Linhas = linhas, TotalListado = 951.00m, QuantidadeValoresNulos = 1 };
        }

        private static string FrxTexto()
        {
            using (var s = typeof(RelatorioListagem).Assembly.GetManifestResourceStream("ERPFinanceiro.Reports.Layouts.ListagemVendas.frx"))
            using (var r = new StreamReader(s)) return r.ReadToEnd();
        }

        private static string Pdf(RelatorioDados d)
        {
            var path = Path.Combine(Path.GetTempPath(), "t49-" + Guid.NewGuid().ToString("N") + ".pdf");
            RelatorioListagem.ExportarPdf(d, Filtro, new DateTime(2026, 9, 21, 14, 3, 0), path);
            return path;
        }

        private static void AssertPdfValido(string path)
        {
            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 1000);
            Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
            var cauda = Encoding.ASCII.GetString(bytes, Math.Max(0, bytes.Length - 32), Math.Min(32, bytes.Length));
            Assert.Contains("%%EOF", cauda);
        }

        [Fact]
        public void Seed_GeraPdfValido()
        {
            var path = Pdf(Seed());
            try { AssertPdfValido(path); }
            finally { File.Delete(path); }
        }

        [Fact]
        public void Seed_PreparaComParametrosDeCabecalhoTotalENota()
        {
            using (var rep = RelatorioListagem.Preparar(Seed(), Filtro, new DateTime(2026, 9, 21, 14, 3, 0)))
            {
                Assert.Equal("951,00", (string)rep.GetParameterValue("TotalListado"));
                Assert.Equal("21/09/2026 14:03", (string)rep.GetParameterValue("DataEmissao"));
                Assert.StartsWith("* 1 venda(s)", (string)rep.GetParameterValue("NotaRodape"));
                Assert.Equal(string.Empty, (string)rep.GetParameterValue("MensagemVazia"));
                Assert.Equal(Filtro, (string)rep.GetParameterValue("FiltroAplicado"));
            }
        }

        [Fact]
        public void ListaVazia_GeraPdfComMensagemNenhumaVenda()
        {
            var vazio = new RelatorioDados { Linhas = new List<LinhaRelatorio>(), TotalListado = 0m };
            using (var rep = RelatorioListagem.Preparar(vazio, Filtro, DateTime.Now))
            {
                Assert.Equal("Nenhuma venda no período", (string)rep.GetParameterValue("MensagemVazia"));
                Assert.Equal("0,00", (string)rep.GetParameterValue("TotalListado"));
            }
            var path = Pdf(vazio);
            try { AssertPdfValido(path); }
            finally { File.Delete(path); }
        }

        [Fact]
        public void PaginaPreparada_TemTresLinhasNoSeedENenhumaNaListaVazia()
        {
            Assert.Equal(new[] { "V-1002", "V-1003", "V-1001" }, VendaIdsImpressos(Seed()));
            Assert.Empty(VendaIdsImpressos(new RelatorioDados { Linhas = new List<LinhaRelatorio>() }));
        }

        private static List<string> VendaIdsImpressos(RelatorioDados d)
        {
            var ids = new List<string>();
            using (var rep = RelatorioListagem.Preparar(d, Filtro, DateTime.Now))
            {
                var pagina = rep.PreparedPages.GetPage(0);
                foreach (FastReport.Base b in pagina.Bands)
                    foreach (FastReport.Base o in ((FastReport.BandBase)b).Objects)
                        if (o is FastReport.TextObject t && t.Name.StartsWith("CVendaId")) ids.Add(t.Text);
            }
            return ids;
        }

        [Fact]
        public void Layout_NaoTemCorHardcoded()
        {
            var frx = FrxTexto();
            Assert.DoesNotMatch(new Regex(@"(Fill\.Color|TextFill\.Color|Border\.Color|Color=|#[0-9A-Fa-f]{6})"), frx);
        }

        [Fact]
        public void Layout_TemColunasEElementosEsperados()
        {
            var frx = FrxTexto();
            foreach (var c in new[] { "VendaId", "ClienteId", "Valor", "Status", "Recebida", "Quitada", "Cancelada", "[Titulo]", "[FiltroAplicado]", "[DataEmissao]", "Total listado", "[NotaRodape]", "[MensagemVazia]" })
                Assert.Contains(c, frx);
        }
    }
}
