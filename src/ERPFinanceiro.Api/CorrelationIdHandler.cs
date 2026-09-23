using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ERPFinanceiro.Api
{
    /// <summary>
    /// Message handler (T-29) do pipeline Web API 2/OWIN: por requisição, lê o
    /// header <c>X-Correlation-Id</c> do request; se ausente, gera um novo
    /// (<see cref="Guid.NewGuid"/>). Popula <see cref="ICorrelationContext"/>
    /// (T-25, via <see cref="CorrelationContext.Definir"/>) antes de a requisição
    /// seguir pipeline abaixo (controller/serviço de aplicação), e devolve o mesmo
    /// valor no header <c>X-Correlation-Id</c> da resposta.
    /// <para>
    /// Registrado em <see cref="Startup"/> como <c>MessageHandlers</c> do
    /// <c>HttpConfiguration</c> — roda para toda requisição, antes do roteamento
    /// de controller.
    /// </para>
    /// <para>
    /// Nota de antecipação: a gravação do <c>CORRELATION_ID</c> no histórico da
    /// venda (regra 8) só existe a partir de T-18 em diante; este handler apenas
    /// popula o <see cref="ICorrelationContext"/> da requisição — não escreve em
    /// nenhum histórico.
    /// </para>
    /// </summary>
    public class CorrelationIdHandler : DelegatingHandler
    {
        public const string HeaderName = "X-Correlation-Id";

        private readonly CorrelationContext _correlationContext;

        public CorrelationIdHandler(CorrelationContext correlationContext)
        {
            _correlationContext = correlationContext
                ?? throw new ArgumentNullException(nameof(correlationContext));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string correlationId = ObterOuGerarCorrelationId(request);

            _correlationContext.Definir(correlationId);

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (!response.Headers.Contains(HeaderName))
            {
                response.Headers.Add(HeaderName, correlationId);
            }

            return response;
        }

        private static string ObterOuGerarCorrelationId(HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues(HeaderName, out var valores))
            {
                foreach (var valor in valores)
                {
                    if (!string.IsNullOrWhiteSpace(valor))
                    {
                        return valor;
                    }
                }
            }

            return Guid.NewGuid().ToString();
        }
    }
}
