using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ERPFinanceiro.Api;
using Xunit;

namespace ERPFinanceiro.Tests.Api
{
    /// <summary>
    /// Critério de aceite de T-35 (S-08, P-4/P-5): sem header e com chave errada devolvem
    /// 401 idêntico (byte a byte); chave correta segue o pipeline (200); GET /api/health é
    /// isento; a chave nunca aparece em log/resposta; comparação em tempo constante.
    /// </summary>
    public class ApiKeyHandlerTests
    {
        private const string ChaveCorreta = "chave-secreta-de-teste-123";

        private class InnerOk : DelegatingHandler
        {
            public int Chamadas { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Chamadas++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        private class ColetorTrace : TraceListener
        {
            public StringBuilder Texto { get; } = new StringBuilder();
            public override void Write(string message) { Texto.Append(message); }
            public override void WriteLine(string message) { Texto.AppendLine(message); }
        }

        private static HttpMessageInvoker Criar(out InnerOk inner, string chave = ChaveCorreta)
        {
            inner = new InnerOk();
            return new HttpMessageInvoker(new ApiKeyHandler(chave) { InnerHandler = inner });
        }

        private static async Task<(HttpStatusCode, string, string)> Enviar(
            HttpMessageInvoker invoker, HttpMethod metodo, string caminho, string chave)
        {
            var req = new HttpRequestMessage(metodo, "http://localhost" + caminho);
            if (chave != null)
            {
                req.Headers.Add(ApiKeyHandler.HeaderName, chave);
            }

            HttpResponseMessage resp = await invoker.SendAsync(req, CancellationToken.None);
            string corpo = resp.Content == null ? "" : await resp.Content.ReadAsStringAsync();
            string tipo = resp.Content?.Headers.ContentType?.ToString();
            return (resp.StatusCode, corpo, tipo);
        }

        [Fact]
        public async Task SemHeaderEChaveErrada_Retornam401IdenticoByteAByte()
        {
            using (HttpMessageInvoker invoker = Criar(out InnerOk inner))
            {
                var semHeader = await Enviar(invoker, HttpMethod.Post, "/api/vendas/quitacao", null);
                var errada = await Enviar(invoker, HttpMethod.Post, "/api/vendas/quitacao", "outra-chave");
                var vazia = await Enviar(invoker, HttpMethod.Post, "/api/vendas/quitacao", "");

                Assert.Equal(HttpStatusCode.Unauthorized, semHeader.Item1);
                Assert.Equal(semHeader, errada);
                Assert.Equal(semHeader, vazia);
                Assert.Equal(
                    @"{""erro"":{""codigo"":""NAO_AUTORIZADO"",""mensagem"":""Credencial de acesso ausente ou inválida.""}}",
                    semHeader.Item2);
                Assert.Equal(0, inner.Chamadas);
            }
        }

        [Fact]
        public async Task ChaveCorreta_SegueOPipeline_200()
        {
            using (HttpMessageInvoker invoker = Criar(out InnerOk inner))
            {
                var r = await Enviar(invoker, HttpMethod.Post, "/api/vendas/quitacao", ChaveCorreta);

                Assert.Equal(HttpStatusCode.OK, r.Item1);
                Assert.Equal(1, inner.Chamadas);
            }
        }

        [Theory]
        [InlineData("/api/health")]
        [InlineData("/api/health/")]
        [InlineData("/API/Health")]
        public async Task GetHealth_EIsentoDeChave(string caminho)
        {
            using (HttpMessageInvoker invoker = Criar(out InnerOk inner))
            {
                var r = await Enviar(invoker, HttpMethod.Get, caminho, null);

                Assert.Equal(HttpStatusCode.OK, r.Item1);
                Assert.Equal(1, inner.Chamadas);
            }
        }

        [Theory]
        [InlineData("POST", "/api/health")]
        [InlineData("GET", "/api/health/extra")]
        [InlineData("GET", "/api/vendas/quitacao")]
        public async Task OutrasRotasOuMetodos_NaoSaoIsentos(string metodo, string caminho)
        {
            using (HttpMessageInvoker invoker = Criar(out InnerOk inner))
            {
                var r = await Enviar(invoker, new HttpMethod(metodo), caminho, null);

                Assert.Equal(HttpStatusCode.Unauthorized, r.Item1);
                Assert.Equal(0, inner.Chamadas);
            }
        }

        [Fact]
        public async Task ChaveNaoConfigurada_FalhaFechado_MesmoComHeaderVazio()
        {
            using (HttpMessageInvoker invoker = Criar(out _, chave: null))
            {
                var r = await Enviar(invoker, HttpMethod.Get, "/api/vendas/x", "");

                Assert.Equal(HttpStatusCode.Unauthorized, r.Item1);
            }
        }

        [Fact]
        public async Task ChaveNuncaApareceEmLogNemNaResposta()
        {
            var coletor = new ColetorTrace();
            Trace.Listeners.Add(coletor);
            try
            {
                using (HttpMessageInvoker invoker = Criar(out _))
                {
                    var errada = await Enviar(invoker, HttpMethod.Get, "/api/x", "tentativa-" + ChaveCorreta);
                    var certa = await Enviar(invoker, HttpMethod.Get, "/api/x", ChaveCorreta);

                    Assert.DoesNotContain(ChaveCorreta, errada.Item2);
                    Assert.DoesNotContain(ChaveCorreta, certa.Item2);
                }
            }
            finally
            {
                Trace.Listeners.Remove(coletor);
            }

            Assert.DoesNotContain(ChaveCorreta, coletor.Texto.ToString());
        }

        [Theory]
        [InlineData("abc", "abc", true)]
        [InlineData("abc", "abd", false)]
        [InlineData("abc", "abcd", false)]
        [InlineData("abc", "", false)]
        [InlineData("abc", null, false)]
        [InlineData(null, "abc", false)]
        [InlineData("", "", false)]
        [InlineData(null, null, false)]
        public void ComparacaoEmTempoConstante_Resultados(string esperada, string recebida, bool esperado)
        {
            Assert.Equal(esperado, ApiKeyHandler.ComparacaoEmTempoConstante(esperada, recebida));
        }
    }
}
