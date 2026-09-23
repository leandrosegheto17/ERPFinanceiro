using System;
using System.Data;
using System.Globalization;
using System.Linq;
using FastReport;
using FastReport.Export.PdfSimple;

namespace ERPFinanceiro.Reports
{
    /// <summary>
    /// Carrega o layout ListagemVendas.frx (T-49), liga os dados de T-48 e exporta PDF.
    /// O fluxo de tela (preview/WaitForm) e T-50.
    /// </summary>
    public static class RelatorioListagem
    {
        public const string MensagemSemVendas = "Nenhuma venda no período";
        private const string Recurso = "ERPFinanceiro.Reports.Layouts.ListagemVendas.frx";
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        /// <param name="filtroAplicado">Texto do filtro (ex.: "Filtro: 01/09/2026 a 21/09/2026 | Cliente: (todos) | Status: (todos)").</param>
        public static Report Preparar(RelatorioDados dados, string filtroAplicado, DateTime emissao)
        {
            if (dados == null) throw new ArgumentNullException(nameof(dados));

            var report = new Report();
            using (var s = typeof(RelatorioListagem).Assembly.GetManifestResourceStream(Recurso))
            {
                if (s == null) throw new InvalidOperationException("Layout embutido não encontrado: " + Recurso);
                report.Load(s);
            }

            // O compilador de expressões do FastReport referencia Microsoft.CSharp pelo assembly já
            // carregado; sem isto o Prepare falha com CS0006 neste host.
            GC.KeepAlive(typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly);

            var tabela = new DataTable("Linhas");
            tabela.Columns.Add("VendaId", typeof(string));
            tabela.Columns.Add("ClienteId", typeof(string));
            tabela.Columns.Add("ValorTotal", typeof(decimal));
            tabela.Columns.Add("ValorNulo", typeof(bool));
            tabela.Columns.Add("Status", typeof(string));
            // Datas ja formatadas (nulo = "—"): o Format de data do FastReport nao trata DBNull.
            tabela.Columns.Add("DataRecebimento", typeof(string));
            tabela.Columns.Add("DataQuitacao", typeof(string));
            tabela.Columns.Add("DataCancelamento", typeof(string));
            foreach (var l in dados.Linhas)
                tabela.Rows.Add(l.VendaId, l.ClienteId, l.ValorTotal, l.ValorNulo, l.Status,
                    FormatarData(l.DataRecebimento), FormatarData(l.DataQuitacao), FormatarData(l.DataCancelamento));
            var ds = new DataSet();
            ds.Tables.Add(tabela);
            report.RegisterData(ds);
            var fonte = report.GetDataSource("Linhas");
            fonte.Enabled = true;
            // O .frx carregado sem a fonte registrada nao resolve DataSource="Linhas"; liga aqui.
            ((DataBand)report.FindObject("Data1")).DataSource = fonte;

            var nota = dados.NotaRodape;
            report.SetParameterValue("Titulo", "RELATÓRIO FINANCEIRO DE VENDAS");
            report.SetParameterValue("FiltroAplicado", filtroAplicado ?? string.Empty);
            report.SetParameterValue("DataEmissao", emissao.ToString("dd/MM/yyyy HH:mm", PtBr));
            report.SetParameterValue("TotalListado", dados.TotalListado.ToString("N2", PtBr));
            report.SetParameterValue("NotaRodape", string.IsNullOrEmpty(nota) ? string.Empty : "* " + nota);
            report.SetParameterValue("MensagemVazia", dados.Linhas.Count == 0 ? MensagemSemVendas : string.Empty);

            report.Prepare();
            return report;
        }

        private static string FormatarData(DateTime? d)
        {
            return d.HasValue ? d.Value.ToString("dd/MM/yyyy HH:mm", PtBr) : "—";
        }

        public static void ExportarPdf(RelatorioDados dados, string filtroAplicado, DateTime emissao, string caminhoPdf)
        {
            using (var report = Preparar(dados, filtroAplicado, emissao))
            using (var export = new PDFSimpleExport())
            {
                export.Export(report, caminhoPdf);
            }
        }
    }
}
