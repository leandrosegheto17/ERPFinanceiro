using System;
using System.Data;
using System.Globalization;
using System.IO;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Domain.Enums;
using FastReport;
using FastReport.Export.PdfSimple;

namespace ERPFinanceiro.Reports
{
    /// <summary>
    /// Gera o PDF do relatório financeiro de vendas (F-5, UX-SPEC 2.3, T-49) a partir do layout
    /// embutido <c>Layouts/RelatorioListagem.frx</c>, com FastReport Open Source (MIT) + export
    /// PdfSimple. Não depende de GUI/preview (funciona headless). O preview WinForms (se houver)
    /// é decisão de T-50/ADR-008 e não é coberto aqui.
    /// <para>
    /// Formatação: <c>Reports</c> não referencia <c>Desktop</c> (SDD 2.1), então as regras de
    /// <c>Formatadores</c> (moeda N2 pt-BR, data local dd/MM/yyyy HH:mm, status em texto) são
    /// replicadas em <see cref="FormatacaoRelatorio"/>; teste garante a mesma saída de ambos.
    /// </para>
    /// </summary>
    public class GeradorRelatorioPdf
    {
        public const string RecursoLayout = "ERPFinanceiro.Reports.Layouts.RelatorioListagem.frx";
        public const string TextoListaVazia = "Nenhuma venda no período";

        /// <summary>Gera o PDF em <paramref name="destino"/>. <paramref name="emitidoEmLocal"/> é hora local.</summary>
        /// <remarks>
        /// O export "PdfSimple" do Open Source rasteriza cada pagina (JPEG 300 dpi dentro do PDF): o PDF
        /// abre e imprime, mas o texto NAO e selecionavel/pesquisavel e o arquivo e grande (~250 KB a 1 MB
        /// por pagina). Fontes embutidas/texto vetorial so no PDF export do FastReport .NET comercial.
        /// </remarks>
        public void Gerar(ResultadoRelatorioVendas resultado, string descricaoFiltro, DateTime emitidoEmLocal, Stream destino)
        {
            if (destino == null) throw new ArgumentNullException(nameof(destino));
            Renderizar(resultado, descricaoFiltro, emitidoEmLocal, new PDFSimpleExport(), destino);
        }

        /// <summary>
        /// Mesma renderizacao exportada como HTML (texto real). Usada nos testes para assercao de
        /// conteudo (o PDF do Open Source e raster, ver <see cref="Gerar(ResultadoRelatorioVendas,string,DateTime,Stream)"/>).
        /// </summary>
        public void GerarHtml(ResultadoRelatorioVendas resultado, string descricaoFiltro, DateTime emitidoEmLocal, Stream destino)
        {
            if (destino == null) throw new ArgumentNullException(nameof(destino));
            Renderizar(resultado, descricaoFiltro, emitidoEmLocal, new FastReport.Export.Html.HTMLExport(), destino);
        }

        private static void Renderizar(ResultadoRelatorioVendas resultado, string descricaoFiltro, DateTime emitidoEmLocal,
            FastReport.Export.ExportBase exportador, Stream destino)
        {
            if (resultado == null) throw new ArgumentNullException(nameof(resultado));

            using (var dados = new DataSet("Dados"))
            using (var report = new Report())
            {
                var tabela = dados.Tables.Add("Linhas");
                foreach (var c in new[] { "VendaId", "ClienteId", "Valor", "Status", "Recebida", "Quitada", "Cancelada" })
                    tabela.Columns.Add(c, typeof(string));

                foreach (var l in resultado.Linhas)
                {
                    tabela.Rows.Add(
                        l.VendaId,
                        FormatacaoRelatorio.Texto(l.ClienteId),
                        FormatacaoRelatorio.Moeda(l.ValorTotal ?? 0m),
                        FormatacaoRelatorio.Status(l.Status),
                        FormatacaoRelatorio.DataHoraLocal(l.DataRecebimento),
                        FormatacaoRelatorio.DataHoraLocal(l.DataQuitacao),
                        FormatacaoRelatorio.DataHoraLocal(l.DataCancelamento));
                }

                using (var layout = typeof(GeradorRelatorioPdf).Assembly.GetManifestResourceStream(RecursoLayout))
                {
                    if (layout == null)
                        throw new InvalidOperationException("Layout do relatório não encontrado: " + RecursoLayout);
                    report.Load(layout);
                }

                report.RegisterData(dados, "Dados");
                report.GetDataSource("Linhas").Enabled = true;

                bool vazio = resultado.Linhas.Count == 0;
                report.SetParameterValue("Filtro", descricaoFiltro ?? string.Empty);
                report.SetParameterValue("EmitidoEm", emitidoEmLocal.ToString("dd/MM/yyyy HH:mm", FormatacaoRelatorio.PtBr));
                report.SetParameterValue("TotalListado", FormatacaoRelatorio.Moeda(resultado.TotalListado));
                report.SetParameterValue("MensagemVazia", vazio ? TextoListaVazia : string.Empty);
                report.SetParameterValue("NotaNulos", FormatacaoRelatorio.NotaNulos(resultado.QuantidadeSemValor));

                report.Prepare();
                report.Export(exportador, destino);
            }
        }

        /// <summary>Gera o PDF em arquivo (sobrescreve).</summary>
        public void Gerar(ResultadoRelatorioVendas resultado, string descricaoFiltro, DateTime emitidoEmLocal, string caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho)) throw new ArgumentException("Caminho obrigatório.", nameof(caminho));
            using (var fs = new FileStream(caminho, FileMode.Create, FileAccess.Write))
                Gerar(resultado, descricaoFiltro, emitidoEmLocal, fs);
        }
    }

    /// <summary>Formatação do relatório (equivalente a <c>Desktop.Formatadores</c>, ver <see cref="GeradorRelatorioPdf"/>).</summary>
    public static class FormatacaoRelatorio
    {
        public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        public static string Moeda(decimal valor) => valor.ToString("N2", PtBr);

        public static string DataHoraLocal(DateTime utc)
        {
            var u = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(u, TimeZoneInfo.Local).ToString("dd/MM/yyyy HH:mm", PtBr);
        }

        public static string DataHoraLocal(DateTime? utc) => utc.HasValue ? DataHoraLocal(utc.Value) : "—";

        /// <summary>Status sempre em TEXTO no PDF (ícone não usado no relatório impresso).</summary>
        public static string Status(StatusVenda status)
        {
            switch (status)
            {
                case StatusVenda.Quitada: return "Quitada";
                case StatusVenda.Pendente: return "Pendente";
                case StatusVenda.Cancelada: return "Cancelada";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, "StatusVenda não mapeado.");
            }
        }

        public static string Texto(string valor) => string.IsNullOrEmpty(valor) ? "—" : valor;

        /// <summary>Nota de rodapé (UX-SPEC 2.3); vazia quando não há nulos.</summary>
        public static string NotaNulos(int quantidade)
        {
            if (quantidade <= 0) return string.Empty;
            return quantidade == 1
                ? "Nota: 1 venda com valor nulo (cancelada sem quitação) entrou como 0,00 no total e foi contada."
                : "Nota: " + quantidade + " vendas com valor nulo (canceladas sem quitação) entraram como 0,00 no total e foram contadas.";
        }
    }
}
