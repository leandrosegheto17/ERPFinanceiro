using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ERPFinanceiro.Api;
using Xunit;

namespace ERPFinanceiro.Tests.Api
{
    /// <summary>
    /// Critério de aceite de T-29: (1) toda resposta traz <c>X-Correlation-Id</c>;
    /// (2) se a requisição já trouxer o header, o mesmo valor é propagado (não gera
    /// um novo); (3) se ausente, um novo é gerado e usado. Invoca o handler
    /// diretamente via <see cref="HttpMessageInvoker"/> com um
    /// <see cref="InnerHandlerDeTeste"/> como próximo elo do pipeline — sem subir o
    /// host OWIN nem precisar de banco (Api pura).
    /// <para>
    /// Nota de antecipação: não testa a gravação do <c>CORRELATION_ID</c> no
    /// histórico da venda (regra 8) — isso só existe a partir de T-18 em diante;
    /// aqui só o comportamento do handler em si (ler/gerar/propagar o header e
    /// popular <see cref="CorrelationContext"/>).
    /// </para>
    /// </summary>
    public class CorrelationIdHandlerTests
    {
        /// <summary>
        /// Inner handler de teste: devolve 200 sem nenhum header próprio, e captura o
        /// <see cref="CorrelationContext.CorrelationId"/> visto "downstream" (como um
        /// controller/serviço de aplicação real veria) — precisa ser lido de dentro do
        /// próprio <see cref="SendAsync"/> do inner handler, não depois que
        /// <see cref="HttpMessageInvoker.SendAsync"/> retorna ao chamador: o valor é
        /// propagado via <see cref="System.Runtime.Remoting.Messaging.CallContext"/>
        /// (logical call context), que flui adiante no mesmo fluxo assíncrono mas não
        /// "volta" para quem chamou o invoker depois do await completar.
        /// </summary>
        private class InnerHandlerDeTeste : DelegatingHandler
        {
            private readonly CorrelationContext _contexto;

            public string CorrelationIdVistoDownstream { get; private set; }

            public InnerHandlerDeTeste(CorrelationContext contexto)
            {
                _contexto = contexto;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CorrelationIdVistoDownstream = _contexto.CorrelationId;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        private static HttpMessageInvoker CriarInvoker(
            out CorrelationContext contexto, out InnerHandlerDeTeste innerHandler)
        {
            contexto = new CorrelationContext();
            innerHandler = new InnerHandlerDeTeste(contexto);
            var handler = new CorrelationIdHandler(contexto)
            {
                InnerHandler = innerHandler
            };
            return new HttpMessageInvoker(handler);
        }

        [Fact]
        public async Task SendAsync_TodaRespostaTrazHeaderXCorrelationId()
        {
            using (HttpMessageInvoker invoker = CriarInvoker(out _, out _))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/qualquer");

                HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

                Assert.True(response.Headers.Contains(CorrelationIdHandler.HeaderName));
            }
        }

        [Fact]
        public async Task SendAsync_RequisicaoComHeader_PropagaMesmoValor_NaoGeraNovo()
        {
            CorrelationContext contexto;
            InnerHandlerDeTeste innerHandler;
            using (HttpMessageInvoker invoker = CriarInvoker(out contexto, out innerHandler))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/qualquer");
                const string correlationIdOriginal = "11111111-1111-1111-1111-111111111111";
                request.Headers.Add(CorrelationIdHandler.HeaderName, correlationIdOriginal);

                HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

                string correlationIdResposta = Assert.Single(
                    response.Headers.GetValues(CorrelationIdHandler.HeaderName));
                Assert.Equal(correlationIdOriginal, correlationIdResposta);
                Assert.Equal(correlationIdOriginal, innerHandler.CorrelationIdVistoDownstream);
            }
        }

        [Fact]
        public async Task SendAsync_RequisicaoSemHeader_GeraNovoEUsaNaResposta()
        {
            CorrelationContext contexto;
            InnerHandlerDeTeste innerHandler;
            using (HttpMessageInvoker invoker = CriarInvoker(out contexto, out innerHandler))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/qualquer");

                HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

                string correlationIdResposta = Assert.Single(
                    response.Headers.GetValues(CorrelationIdHandler.HeaderName));
                Assert.False(string.IsNullOrWhiteSpace(correlationIdResposta));
                Assert.True(System.Guid.TryParse(correlationIdResposta, out _));
                Assert.Equal(correlationIdResposta, innerHandler.CorrelationIdVistoDownstream);
            }
        }
    }
}
