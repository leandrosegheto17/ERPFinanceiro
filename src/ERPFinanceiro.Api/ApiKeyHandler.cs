using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ERPFinanceiro.Api.Erros;

namespace ERPFinanceiro.Api
{
    /// <summary>
    /// Autenticação por chave (T-35, S-08, P-4/P-5; SDD Seção 7): exige o header
    /// <c>X-Api-Key</c> em toda requisição exceto <c>GET /api/health</c>. Ausente,
    /// vazia ou inválida -> 401 <c>NAO_AUTORIZADO</c> com resposta idêntica byte a byte
    /// (não revela qual caso). Comparação em tempo constante. Falha fechada: se nenhuma
    /// chave estiver configurada, nada é autorizado. Este handler NUNCA loga nem ecoa a
    /// chave (nem a esperada nem a recebida).
    /// <para>
    /// Posição no pipeline: registrado em <see cref="Startup"/> logo depois de
    /// <see cref="CorrelationIdHandler"/> (T-29) — assim o 401 também carrega
    /// <c>X-Correlation-Id</c> — e antes do roteamento/controller.
    /// </para>
    /// </summary>
    public class ApiKeyHandler : DelegatingHandler
    {
        public const string HeaderName = "X-Api-Key";
        public const string CodigoErro = "NAO_AUTORIZADO";
        public const string MensagemErro = "Credencial de acesso ausente ou inválida.";
        private const string CaminhoHealth = "/api/health";

        private readonly string _chaveEsperada;

        public ApiKeyHandler(string chaveEsperada)
        {
            _chaveEsperada = chaveEsperada;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (EhHealth(request) || Autorizada(request))
            {
                return base.SendAsync(request, cancellationToken);
            }

            return Task.FromResult(CriarResposta401(request));
        }

        private bool Autorizada(HttpRequestMessage request)
        {
            string recebida = null;
            if (request.Headers.TryGetValues(HeaderName, out var valores))
            {
                foreach (string v in valores)
                {
                    recebida = v;
                    break;
                }
            }

            return ComparacaoEmTempoConstante(_chaveEsperada, recebida);
        }

        private static bool EhHealth(HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Get)
            {
                return false;
            }

            string caminho = request.RequestUri?.AbsolutePath ?? string.Empty;
            return string.Equals(caminho.TrimEnd('/'), CaminhoHealth, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compara via SHA-256 de ambos (tamanho fixo, não vaza comprimento) e OR-acumula
        /// as diferenças byte a byte sem retorno antecipado. Nula/vazia nunca autoriza.
        /// </summary>
        public static bool ComparacaoEmTempoConstante(string esperada, string recebida)
        {
            bool validas = !string.IsNullOrEmpty(esperada) && !string.IsNullOrEmpty(recebida);

            byte[] a;
            byte[] b;
            using (var sha = SHA256.Create())
            {
                a = sha.ComputeHash(Encoding.UTF8.GetBytes(esperada ?? string.Empty));
                b = sha.ComputeHash(Encoding.UTF8.GetBytes(recebida ?? string.Empty));
            }

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return validas & (diff == 0);
        }

        private static HttpResponseMessage CriarResposta401(HttpRequestMessage request)
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
    }
}
