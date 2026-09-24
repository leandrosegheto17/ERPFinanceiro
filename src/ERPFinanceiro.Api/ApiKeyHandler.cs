using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using ERPFinanceiro.Api.Erros;
using ERPFinanceiro.Application.Interfaces;

namespace ERPFinanceiro.Api
{
    /// <summary>
    /// Message handler (T-35, S-08): exige o header <c>X-Api-Key</c> em toda requisição,
    /// exceto <c>GET /api/health</c> (T-36, contrato v1.1 Seção 2/P-4/P-5). Registrado em
    /// <see cref="Startup"/> via <c>config.MessageHandlers.Add(...)</c>, mesmo padrão de
    /// <see cref="CorrelationIdHandler"/> (T-29) — roda antes do roteamento de controller,
    /// então nenhuma rota autenticável escapa da checagem (nota de
    /// <c>SECURITY-REVIEW.md</c> sobre esta tarefa).
    /// <para>
    /// Ausente ou inválida: sempre 401 com o mesmo envelope
    /// <c>{erro:{codigo:"NAO_AUTORIZADO",mensagem}}</c> (`docs/contrato-v1.1.md` Seção 2/3.1) —
    /// os dois casos não são distinguíveis pelo chamador (regra do contrato v1.1: "resposta
    /// idêntica nos dois casos").
    /// </para>
    /// <para>
    /// Comparação em tempo constante: <c>System.Security.Cryptography.CryptographicOperations.FixedTimeEquals</c>
    /// só existe a partir de .NET Core 2.1/.NET Standard 2.1 — fora do alcance do net48
    /// (ADR-004/SDD 2.1, target da Api). Implementado manualmente em
    /// <see cref="ComparacaoEmTempoConstante"/>: percorre sempre o maior dos dois arrays de
    /// bytes (nunca retorna cedo por causa do tamanho) e acumula a diferença num único
    /// inteiro via XOR, só decidindo o resultado ao final — evita vazar, por timing, quantos
    /// bytes da chave recebida coincidem com a chave configurada.
    /// </para>
    /// <para>
    /// A chave nunca é logada: <see cref="RegistrarTentativaNaoAutorizada"/> grava só uma
    /// mensagem genérica (TASK.md Seção 1, regra 7; GUARDRAILS G-6.24; SDD Seção
    /// "Segredos/logs") — nem o valor recebido, nem o valor configurado.
    /// </para>
    /// </summary>
    public class ApiKeyHandler : DelegatingHandler
    {
        public const string HeaderName = "X-Api-Key";

        private const string ChaveConfiguracao = "Api:ApiKey";
        private const string CodigoErro = "NAO_AUTORIZADO";
        private const string MensagemErro = "Credencial de acesso ausente ou inválida.";

        /// <summary>
        /// Rota isenta de autenticação (T-36, contrato v1.1 Seção 2/P-5). Comparação por
        /// <c>AbsolutePath</c> — a rota pode ainda não responder 200 (ex. T-36 não terminada
        /// nesta chamada específica); isenção é por convenção de rota, não pela existência
        /// do endpoint.
        /// </summary>
        private const string RotaIsenta = "/api/health";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (EstaIsenta(request) || ChaveValida(request))
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            RegistrarTentativaNaoAutorizada(request);

            return ConstruirRespostaNaoAutorizada(request);
        }

        private static bool EstaIsenta(HttpRequestMessage request)
        {
            string caminho = request?.RequestUri?.AbsolutePath ?? string.Empty;
            caminho = caminho.TrimEnd('/');

            return string.Equals(caminho, RotaIsenta, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ChaveValida(HttpRequestMessage request)
        {
            string chaveConfigurada = ObterChaveConfigurada();
            if (string.IsNullOrEmpty(chaveConfigurada))
            {
                // Sem chave configurada, nenhuma requisição autenticável pode passar —
                // nunca "abre" a Api por omissão de configuração.
                return false;
            }

            if (request?.Headers == null || !request.Headers.TryGetValues(HeaderName, out var valores))
            {
                return false;
            }

            string chaveRecebida = valores.FirstOrDefault() ?? string.Empty;

            return ComparacaoEmTempoConstante(chaveConfigurada, chaveRecebida);
        }

        private static string ObterChaveConfigurada()
        {
            return ConfigurationManager.AppSettings[ChaveConfiguracao];
        }

        /// <summary>
        /// Ver remarks da classe: comparação manual, sempre O(max(len)), nunca retorna cedo.
        /// </summary>
        private static bool ComparacaoEmTempoConstante(string esperada, string recebida)
        {
            byte[] bytesEsperada = Encoding.UTF8.GetBytes(esperada ?? string.Empty);
            byte[] bytesRecebida = Encoding.UTF8.GetBytes(recebida ?? string.Empty);

            int tamanho = Math.Max(bytesEsperada.Length, bytesRecebida.Length);
            int diferenca = bytesEsperada.Length ^ bytesRecebida.Length;

            for (int i = 0; i < tamanho; i++)
            {
                byte bytePorAtual = i < bytesEsperada.Length ? bytesEsperada[i] : (byte)0;
                byte byteRecebidoAtual = i < bytesRecebida.Length ? bytesRecebida[i] : (byte)0;
                diferenca |= bytePorAtual ^ byteRecebidoAtual;
            }

            return diferenca == 0;
        }

        private static HttpResponseMessage ConstruirRespostaNaoAutorizada(HttpRequestMessage request)
        {
            var envelope = new ErroEnvelopeDto
            {
                Erro = new ErroDto { Codigo = CodigoErro, Mensagem = MensagemErro }
            };

            var formatter = new JsonMediaTypeFormatter
            {
                SerializerSettings = Startup.CriarConfiguracaoJson()
            };

            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new ObjectContent<ErroEnvelopeDto>(envelope, formatter),
                RequestMessage = request
            };
        }

        /// <summary>
        /// Registra só um evento genérico (nunca o valor do header recebido nem a chave
        /// configurada) — mesma disciplina de <see cref="Erros.GlobalExceptionHandler"/>
        /// (T-28): resolve <see cref="IAppLogger"/>/<see cref="ICorrelationContext"/> via
        /// <see cref="HttpRequestMessageExtensions.GetDependencyScope"/> (Autofac por
        /// requisição), sem exigir os dois via construtor deste <see cref="DelegatingHandler"/>
        /// (que é instanciado uma vez, fora do escopo por requisição, em
        /// <see cref="Startup.Configuration"/>). Se a resolução falhar (container sem essas
        /// interfaces), a resposta 401 ainda é devolvida corretamente — só o log fica ausente.
        /// </summary>
        private static void RegistrarTentativaNaoAutorizada(HttpRequestMessage request)
        {
            var scope = request?.GetDependencyScope();
            var logger = scope?.GetService(typeof(IAppLogger)) as IAppLogger;
            var correlationContext = scope?.GetService(typeof(ICorrelationContext)) as ICorrelationContext;

            logger?.Registrar(
                "Requisição rejeitada: X-Api-Key ausente ou inválida.",
                correlationContext?.CorrelationId);
        }
    }
}
