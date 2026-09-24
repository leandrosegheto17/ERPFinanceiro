using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Reports;
using Xunit;

namespace ERPFinanceiro.Tests.Reports
{
    /// <summary>T-60: bloco de totais por status do layout (S-07). Sem Firebird; conteudo conferido no HTML da mesma renderizacao.</summary>
    public class GeradorRelatorioTotaisTests
    {
        private static string Normalizar(string s) => Regex.Replace(s, @"\s+", " ");
        private static string HtmlNormalizado(ResultadoRelatorioVendas r, TotaisRelatorioVendas totais)
        {
            using (var ms = new MemoryStream())
            {
                new GeradorRelatorioPdf().GerarHtml(r, "(sem filtro)", new DateTime(2026, 9, 21, 14, 3, 0), ms, totais);
                string html = Encoding.UTF8.GetString(ms.ToArray());
                html = Regex.Replace(html, "<style.*?</style>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                return Normalizar(System.Net.WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")));
            }
        }

        private static ResultadoRelatorioVendas ResultadoSimples() =>
            new ResultadoRelatorioVendas(new List<LinhaRelatorio>
            {
                new LinhaRelatorio { VendaId = "V-1", ClienteId = "C", ValorTotal = 12500m, Status = StatusVenda.Quitada, DataRecebimento = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc) },
                new LinhaRelatorio { VendaId = "V-2", ClienteId = "C", ValorTotal = 800m, Status = StatusVenda.Cancelada, DataRecebimento = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc) },
                new LinhaRelatorio { VendaId = "V-3", ClienteId = "C", ValorTotal = 1900m, Status = StatusVenda.Pendente, DataRecebimento = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc) }
            }, 15200m, 0);

        [Fact]
        public void Gerar_ComTotais_MostraQuitadoECanceladoComContagem()
        {
            var totais = new TotaisRelatorioVendas(new TotalPorStatus(12500m, 1), new TotalPorStatus(800m, 1), null, 15200m);
            string texto = HtmlNormalizado(ResultadoSimples(), totais);
            Assert.Contains("Total quitado: R$ 12.500,00 (1 venda)", texto);
            Assert.Contains("Total cancelado: R$ 800,00 (1 venda)", texto);
            Assert.Contains("Total listado: R$ 15.200,00", texto);
            Assert.DoesNotContain("Total pendente", texto);
        }

        [Fact]
        public void Gerar_ComTotalPendente_MostraLinhaPendente()
        {
            var totais = new TotaisRelatorioVendas(new TotalPorStatus(12500m, 3), new TotalPorStatus(800m, 2), new TotalPorStatus(1900m, 1), 15200m);
            string texto = HtmlNormalizado(ResultadoSimples(), totais);
            Assert.Contains("Total quitado: R$ 12.500,00 (3 vendas)", texto);
            Assert.Contains("Total cancelado: R$ 800,00 (2 vendas)", texto);
            Assert.Contains("Total pendente: R$ 1.900,00 (1 venda)", texto);
        }

        [Fact]
        public void Gerar_SemTotais_NaoMostraBlocoPorStatus()
        {
            string texto = HtmlNormalizado(ResultadoSimples(), null);
            Assert.DoesNotContain("Total quitado", texto);
            Assert.DoesNotContain("Total cancelado", texto);
            Assert.DoesNotContain("Total pendente", texto);
            Assert.Contains("Total listado: R$ 15.200,00", texto);
        }
    }
}
