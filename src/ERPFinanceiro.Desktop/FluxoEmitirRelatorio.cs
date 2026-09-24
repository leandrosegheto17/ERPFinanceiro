using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Reports;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Ponto de extensão do preview do relatório (ADR-008, UX-SPEC 2.3). No build atual
    /// (FastReport Open Source 2023.3.13, MIT) NÃO existe preview WinForms (verificado em T-49),
    /// logo <see cref="ApresentadorPreviewIndisponivel"/> informa <c>Disponivel = false</c> e o
    /// fluxo cai em "PDF em pasta temporária + abrir/mostrar caminho". Uma edição com preview
    /// (FastReport .NET comercial/trial) só precisa fornecer outra implementação desta interface.
    /// </summary>
    public interface IApresentadorPreviewRelatorio
    {
        bool Disponivel { get; }
        void Exibir(ResultadoRelatorioVendas resultado, string descricaoFiltro, DateTime emitidoEmLocal);
    }

    /// <summary>Build Open Source: sem preview real.</summary>
    public sealed class ApresentadorPreviewIndisponivel : IApresentadorPreviewRelatorio
    {
        public bool Disponivel => false;
        public void Exibir(ResultadoRelatorioVendas resultado, string descricaoFiltro, DateTime emitidoEmLocal)
            => throw new NotSupportedException("Preview não disponível nesta edição do FastReport (Open Source).");
    }

    /// <summary>Abre um arquivo no visualizador padrão do sistema (atrás de interface para teste).</summary>
    public interface IAbridorArquivo
    {
        void Abrir(string caminho);
    }

    /// <summary>Só abre o PDF gerado pelo app (shell execute do arquivo); nada além disso é executado.</summary>
    public sealed class AbridorArquivoSistema : IAbridorArquivo
    {
        public void Abrir(string caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
                throw new FileNotFoundException("Arquivo inexistente.", caminho);
            Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true })?.Dispose();
        }
    }

    public enum TipoResultadoRelatorio
    {
        PdfAberto,        // PDF gerado e aberto no visualizador
        PdfGeradoSemAbrir,// PDF gerado, visualizador indisponível: mostrar o caminho
        PreviewExibido,   // ramo com preview (ponto de extensão)
        Cancelado,
        ErroArquivo,      // causa + caminho
        ErroDados         // banco/consulta
    }

    /// <summary>Resultado tipado do fluxo F-5. <see cref="Mensagem"/> nunca contém stack trace.</summary>
    public sealed class ResultadoEmissaoRelatorio
    {
        public TipoResultadoRelatorio Tipo { get; set; }
        public string Caminho { get; set; }
        public string Mensagem { get; set; }
        public bool ListaVazia { get; set; }
        public bool Sucesso => Tipo == TipoResultadoRelatorio.PdfAberto || Tipo == TipoResultadoRelatorio.PdfGeradoSemAbrir
                               || Tipo == TipoResultadoRelatorio.PreviewExibido;
    }

    public static class TextosRelatorio
    {
        public const string Botao = "&Emitir relatório";
        public const string Aguarde = "Gerando relatório...";
        public const string Cancelar = "Cancelar";
        public const string StatusGerado = "Relatório gerado";
        public const string TituloDialogo = "Relatório financeiro de vendas";
        public const string ErroDados = "Não foi possível consultar o banco para gerar o relatório.";
        /// <summary>Chave de appSettings que força o modo "sem preview" (PDF em pasta temporária).</summary>
        public const string ChaveForcarSemPreview = "Relatorio:PreviewDesativado";
    }

    /// <summary>Descrição do filtro no cabeçalho do relatório (UX-SPEC 2.3): período, cliente, status.</summary>
    public static class DescricaoFiltro
    {
        public static string De(FiltroVendas filtro)
        {
            filtro = filtro ?? new FiltroVendas();
            string periodo;
            if (!filtro.PeriodoInicioUtc.HasValue && !filtro.PeriodoFimUtc.HasValue) periodo = "(todos)";
            else
                periodo = (filtro.PeriodoInicioUtc.HasValue ? Formatadores.FormatarDataHoraLocal(filtro.PeriodoInicioUtc.Value).Substring(0, 10) : "...")
                          + " a " + (filtro.PeriodoFimUtc.HasValue ? Formatadores.FormatarDataHoraLocal(filtro.PeriodoFimUtc.Value).Substring(0, 10) : "...");
            string cliente = string.IsNullOrWhiteSpace(filtro.ClienteId) ? "(todos)" : filtro.ClienteId;
            string status = filtro.Status.HasValue ? filtro.Status.Value.ToString() : "(todos)";
            return "Período: " + periodo + " | Cliente: " + cliente + " | Status: " + status;
        }
    }

    /// <summary>
    /// Fluxo F-5 "Emitir relatório" (T-50) sem tipos de UI: dependências injetáveis, resultado tipado.
    /// Roda em thread de fundo (regra 5), respeita <see cref="CancellationToken"/> e nunca deixa PDF
    /// parcial: gera em arquivo ".tmp" e só renomeia para ".pdf" ao terminar.
    /// Limpeza: a cada emissão remove, em melhor esforço, PDFs "RelatorioVendas_*.pdf" com mais de
    /// <see cref="RetencaoPadrao"/> dias na pasta do app; arquivos recentes ou abertos/bloqueados
    /// (IOException) são preservados, então nada que o usuário esteja vendo é apagado.
    /// </summary>
    public sealed class FluxoEmitirRelatorio
    {
        public static readonly TimeSpan RetencaoPadrao = TimeSpan.FromDays(7);
        public const string Prefixo = "RelatorioVendas_";

        private readonly Func<FiltroVendas, ResultadoRelatorioVendas> _obterDados;
        private readonly Action<ResultadoRelatorioVendas, string, DateTime, string> _gerarPdf;
        private readonly Func<string> _pastaTemporaria;
        private readonly IAbridorArquivo _abridor;
        private readonly IApresentadorPreviewRelatorio _preview;
        private readonly Func<DateTime> _agoraLocal;
        private readonly bool _previewDesativado;
        private readonly Action<Exception> _aoFalhar;

        public FluxoEmitirRelatorio(
            Func<FiltroVendas, ResultadoRelatorioVendas> obterDados,
            Action<ResultadoRelatorioVendas, string, DateTime, string> gerarPdf,
            Func<string> pastaTemporaria,
            IAbridorArquivo abridor,
            IApresentadorPreviewRelatorio preview,
            Func<DateTime> agoraLocal,
            bool previewDesativado,
            Action<Exception> aoFalhar = null)
        {
            _obterDados = obterDados ?? throw new ArgumentNullException(nameof(obterDados));
            _gerarPdf = gerarPdf ?? throw new ArgumentNullException(nameof(gerarPdf));
            _pastaTemporaria = pastaTemporaria ?? throw new ArgumentNullException(nameof(pastaTemporaria));
            _abridor = abridor ?? throw new ArgumentNullException(nameof(abridor));
            _preview = preview ?? throw new ArgumentNullException(nameof(preview));
            _agoraLocal = agoraLocal ?? throw new ArgumentNullException(nameof(agoraLocal));
            _previewDesativado = previewDesativado;
            _aoFalhar = aoFalhar;
        }

        /// <summary>Pasta padrão: <c>%TEMP%\ERPFinanceiro\Relatorios</c>.</summary>
        public static string PastaTemporariaPadrao() => Path.Combine(Path.GetTempPath(), "ERPFinanceiro", "Relatorios");

        /// <summary>Lê <c>Relatorio:PreviewDesativado</c> (true/1 = força modo sem preview); ausente/inválido = false.</summary>
        public static bool LerPreviewDesativado(string valorConfig)
        {
            if (string.IsNullOrWhiteSpace(valorConfig)) return false;
            valorConfig = valorConfig.Trim();
            return valorConfig == "1" || valorConfig.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        internal bool UsaPreview => !_previewDesativado && _preview.Disponivel;

        public Task<ResultadoEmissaoRelatorio> EmitirAsync(FiltroVendas filtro, CancellationToken ct)
        {
            return Task.Run(() => Emitir(filtro, ct));
        }

        internal ResultadoEmissaoRelatorio Emitir(FiltroVendas filtro, CancellationToken ct)
        {
            ResultadoRelatorioVendas dados;
            try
            {
                ct.ThrowIfCancellationRequested();
                dados = _obterDados(filtro ?? new FiltroVendas());
            }
            catch (OperationCanceledException) { return Cancelado(); }
            catch (Exception ex)
            {
                Registrar(ex);
                return new ResultadoEmissaoRelatorio { Tipo = TipoResultadoRelatorio.ErroDados, Mensagem = TextosRelatorio.ErroDados };
            }

            if (ct.IsCancellationRequested) return Cancelado();

            DateTime agora = _agoraLocal();
            string descricao = DescricaoFiltro.De(filtro);
            bool vazio = dados.Linhas.Count == 0;

            if (UsaPreview)
            {
                try
                {
                    _preview.Exibir(dados, descricao, agora);
                    return new ResultadoEmissaoRelatorio { Tipo = TipoResultadoRelatorio.PreviewExibido, ListaVazia = vazio };
                }
                catch (Exception ex)
                {
                    // Preview falhou: cai no PDF em vez de perder a emissão.
                    Registrar(ex);
                }
            }

            string pasta = null;
            string final = null;
            string parcial = null;
            try
            {
                pasta = _pastaTemporaria();
                Directory.CreateDirectory(pasta);
                LimparAntigos(pasta, agora);
                string nome = Prefixo + agora.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                final = Path.Combine(pasta, nome + ".pdf");
                parcial = Path.Combine(pasta, nome + ".tmp");

                _gerarPdf(dados, descricao, agora, parcial);
                if (ct.IsCancellationRequested)
                {
                    Apagar(parcial);
                    return Cancelado();
                }
                File.Move(parcial, final);
            }
            catch (Exception ex)
            {
                Apagar(parcial);
                Apagar(final);
                Registrar(ex);
                string alvo = final ?? pasta ?? "(pasta temporária indisponível)";
                return new ResultadoEmissaoRelatorio
                {
                    Tipo = TipoResultadoRelatorio.ErroArquivo,
                    Caminho = alvo,
                    Mensagem = "Não foi possível gerar o arquivo do relatório. Causa: " + CausaResumida(ex) + "\nArquivo: " + alvo,
                    ListaVazia = vazio
                };
            }

            try
            {
                _abridor.Abrir(final);
                return new ResultadoEmissaoRelatorio { Tipo = TipoResultadoRelatorio.PdfAberto, Caminho = final, ListaVazia = vazio };
            }
            catch (Exception ex)
            {
                Registrar(ex);
                return new ResultadoEmissaoRelatorio
                {
                    Tipo = TipoResultadoRelatorio.PdfGeradoSemAbrir,
                    Caminho = final,
                    ListaVazia = vazio,
                    Mensagem = "O relatório foi gerado, mas não foi possível abri-lo no visualizador de PDF. Causa: "
                               + CausaResumida(ex) + "\nArquivo: " + final
                };
            }
        }

        private static ResultadoEmissaoRelatorio Cancelado() =>
            new ResultadoEmissaoRelatorio { Tipo = TipoResultadoRelatorio.Cancelado, Mensagem = "Geração do relatório cancelada." };

        /// <summary>Mensagem curta em pt-BR, sem stack trace.</summary>
        internal static string CausaResumida(Exception ex)
        {
            if (ex is UnauthorizedAccessException) return "sem permissão de acesso à pasta ou ao arquivo.";
            if (ex is DirectoryNotFoundException) return "pasta não encontrada.";
            if (ex is PathTooLongException) return "caminho longo demais.";
            if (ex is FileNotFoundException) return "arquivo não encontrado.";
            if (ex is IOException) return "arquivo em uso ou erro de gravação (" + PrimeiraLinha(ex.Message) + ").";
            return PrimeiraLinha(ex.Message);
        }

        private static string PrimeiraLinha(string s)
        {
            if (string.IsNullOrEmpty(s)) return "erro desconhecido";
            int i = s.IndexOfAny(new[] { '\r', '\n' });
            return i < 0 ? s : s.Substring(0, i);
        }

        private void LimparAntigos(string pasta, DateTime agoraLocal)
        {
            try
            {
                foreach (string f in Directory.GetFiles(pasta, Prefixo + "*"))
                {
                    string ext = Path.GetExtension(f);
                    if (!ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        if (agoraLocal - File.GetLastWriteTime(f) > RetencaoPadrao) File.Delete(f);
                    }
                    catch (Exception) { /* aberto/bloqueado ou sem permissão: preserva */ }
                }
            }
            catch (Exception) { /* limpeza é melhor esforço */ }
        }

        private static void Apagar(string caminho)
        {
            try { if (caminho != null && File.Exists(caminho)) File.Delete(caminho); } catch (Exception) { }
        }

        private void Registrar(Exception ex)
        {
            try { _aoFalhar?.Invoke(ex); } catch (Exception) { }
        }
    }
}
