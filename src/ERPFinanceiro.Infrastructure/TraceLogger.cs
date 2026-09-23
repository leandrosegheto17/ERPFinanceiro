using System;
using System.Diagnostics;
using System.IO;
using ERPFinanceiro.Application.Interfaces;

namespace ERPFinanceiro.Infrastructure
{
    /// <summary>
    /// Implementação de <see cref="IAppLogger"/> (T-21): grava cada linha de log em
    /// arquivo texto simples via <see cref="System.Diagnostics.Trace"/> — um
    /// <see cref="TextWriterTraceListener"/> registrado em <see cref="Trace.Listeners"/>
    /// apontando para <paramref name="caminhoArquivoLog"/> (ver construtor). S-10
    /// (Serilog) permanece Tier C: esta classe não usa nenhuma biblioteca de logging
    /// externa, só a API padrão do .NET Framework.
    ///
    /// O caminho do arquivo é configurável, nunca hardcoded: é recebido via
    /// construtor. O composition root (T-26, Autofac) é responsável por ler o valor
    /// real de <c>App.config</c> (chave sugerida <c>Log:CaminhoArquivo</c>, ver
    /// <c>App.config.example</c>) e repassá-lo aqui.
    ///
    /// Contrato de segurança (regra 7, TASK.md Seção 1 / GUARDRAILS G-6): o
    /// <c>TraceLogger</c> grava exatamente a <c>mensagem</c> recebida em
    /// <see cref="Registrar"/>, sem qualquer tentativa de mascarar, redigir ou
    /// inspecionar o conteúdo — ele não sabe o que é "ApiKey" nem qualquer outro
    /// segredo e não deve saber. A responsabilidade de NUNCA incluir <c>ApiKey</c>
    /// ou qualquer segredo na mensagem passada a <see cref="Registrar"/> é
    /// inteiramente de quem chama (ver <see cref="IAppLogger"/>), nunca deste
    /// logger.
    /// </summary>
    public sealed class TraceLogger : IAppLogger, IDisposable
    {
        private readonly TextWriterTraceListener _listener;
        private readonly object _sync = new object();
        private bool _disposed;

        /// <param name="caminhoArquivoLog">
        /// Caminho completo do arquivo de log (diretório é criado se ausente).
        /// Obrigatório e configurável pelo chamador — nunca hardcoded aqui.
        /// </param>
        public TraceLogger(string caminhoArquivoLog)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivoLog))
            {
                throw new ArgumentException(
                    "Caminho do arquivo de log é obrigatório (configurável via App.config, " +
                    "chave sugerida Log:CaminhoArquivo — ver App.config.example).",
                    nameof(caminhoArquivoLog));
            }

            string diretorio = Path.GetDirectoryName(caminhoArquivoLog);
            if (!string.IsNullOrEmpty(diretorio) && !Directory.Exists(diretorio))
            {
                Directory.CreateDirectory(diretorio);
            }

            _listener = new TextWriterTraceListener(caminhoArquivoLog);
            Trace.Listeners.Add(_listener);
            Trace.AutoFlush = true;
        }

        /// <summary>
        /// Grava uma linha de log com timestamp UTC, <paramref name="correlationId"/> e
        /// <paramref name="mensagem"/> exatamente como recebida — sem mascaramento,
        /// sem redigir nada. Ver contrato de segurança no XML doc da classe.
        /// </summary>
        public void Registrar(string mensagem, string correlationId)
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                Trace.WriteLine($"{DateTime.UtcNow:O} [{correlationId}] {mensagem}");
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Trace.Listeners.Remove(_listener);
                _listener.Flush();
                _listener.Close();
                _listener.Dispose();
            }
        }
    }
}
