using System;
using System.IO;
using ERPFinanceiro.Infrastructure;
using Xunit;

namespace ERPFinanceiro.Tests.Infrastructure
{
    /// <summary>
    /// Testes reais de I/O de arquivo (T-21) — sem Firebird, sem mocks: escreve no
    /// arquivo de log via <see cref="TraceLogger"/> e lê de volta para conferir o
    /// conteúdo gravado.
    /// </summary>
    public class TraceLoggerTests : IDisposable
    {
        private readonly string _caminhoArquivo;

        public TraceLoggerTests()
        {
            _caminhoArquivo = Path.Combine(
                Path.GetTempPath(),
                $"erpfinanceiro-tracelogger-tests-{Guid.NewGuid():N}",
                "app.log");
        }

        public void Dispose()
        {
            string diretorio = Path.GetDirectoryName(_caminhoArquivo);
            if (!string.IsNullOrEmpty(diretorio) && Directory.Exists(diretorio))
            {
                Directory.Delete(diretorio, recursive: true);
            }
        }

        [Fact]
        public void Registrar_GravaLinhaComMensagemECorrelationId()
        {
            const string correlationId = "corr-abc-123";
            const string mensagem = "Venda registrada com sucesso.";

            using (var logger = new TraceLogger(_caminhoArquivo))
            {
                logger.Registrar(mensagem, correlationId);
            }

            string conteudo = File.ReadAllText(_caminhoArquivo);

            Assert.Contains(mensagem, conteudo);
            Assert.Contains(correlationId, conteudo);
        }

        [Fact]
        public void Registrar_CriaDiretorioDoArquivoDeLogSeAusente()
        {
            Assert.False(Directory.Exists(Path.GetDirectoryName(_caminhoArquivo)));

            using (var logger = new TraceLogger(_caminhoArquivo))
            {
                logger.Registrar("mensagem qualquer", "corr-1");
            }

            Assert.True(File.Exists(_caminhoArquivo));
        }

        [Fact]
        public void Construtor_LancaExcecaoQuandoCaminhoNaoInformado()
        {
            Assert.Throws<ArgumentException>(() => new TraceLogger(null));
            Assert.Throws<ArgumentException>(() => new TraceLogger(string.Empty));
            Assert.Throws<ArgumentException>(() => new TraceLogger("   "));
        }

        /// <summary>
        /// Fronteira de responsabilidade (regra 7, TASK.md Seção 1 / GUARDRAILS G-6):
        /// o <see cref="TraceLogger"/> não mascara nem trata de forma especial
        /// conteúdo parecido com um segredo — ele grava exatamente a mensagem
        /// recebida. Isto documenta, em teste, que "nunca logar ApiKey" é
        /// disciplina de quem chama <see cref="TraceLogger.Registrar"/>, não algo
        /// que o logger detecta ou impede sozinho: se o chamador (indevidamente)
        /// passasse um valor de ApiKey na mensagem, ele apareceria integralmente no
        /// arquivo — por isso a Api (chamador real) nunca deve fazer isso.
        /// </summary>
        [Fact]
        public void Registrar_NaoMascaraNemAlteraConteudoDaMensagem_ResponsabilidadeENuncaPassarSegredoEDoChamador()
        {
            const string correlationId = "corr-seguranca-1";
            const string valorParecidoComApiKey = "sk-simulada-1234567890abcdef";
            string mensagemSimulandoChamadaIndevida =
                $"Falha ao autenticar requisição com ApiKey={valorParecidoComApiKey}";

            using (var logger = new TraceLogger(_caminhoArquivo))
            {
                // O TraceLogger grava a mensagem tal como recebida — não é ele quem
                // decide se um segredo pode ou não estar na mensagem; quem chama
                // (a Api) é quem nunca deve montar uma mensagem assim.
                logger.Registrar(mensagemSimulandoChamadaIndevida, correlationId);
            }

            string conteudo = File.ReadAllText(_caminhoArquivo);

            Assert.Contains(mensagemSimulandoChamadaIndevida, conteudo);
            Assert.Contains(valorParecidoComApiKey, conteudo);
            Assert.DoesNotContain("***", conteudo);
            Assert.DoesNotContain("[REDACTED]", conteudo);
        }
    }
}
