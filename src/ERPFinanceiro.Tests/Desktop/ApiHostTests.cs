using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using ERPFinanceiro.Desktop;
using Owin;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// Critério de aceite de T-27 (Lote 6): (1) subir em porta livre responde 200 num
    /// GET de teste; (2) porta ocupada não derruba o app e o estado reporta
    /// <see cref="EstadoApiHost.InativaPortaEmUso"/>; (3) <see cref="ApiHost.Stop"/>
    /// libera a porta (confirmado reabrindo-a). Usa a sobrecarga
    /// <c>ApiHost.Start(int, Action&lt;IAppBuilder&gt;)</c> (internal, visível via
    /// <c>InternalsVisibleTo</c> no <c>.csproj</c> do Desktop) com um pipeline OWIN
    /// mínimo — nenhum controller Web API real existe ainda (T-30/T-31/T-32 são
    /// tarefas futuras), então o "GET de teste" do critério de aceite é exercitado
    /// contra esse pipeline mínimo, não contra <see cref="ERPFinanceiro.Api.Startup"/>
    /// (isso é testado à parte, no teste de conexão com o container real abaixo).
    /// </summary>
    public class ApiHostTests
    {
        private static readonly Action<IAppBuilder> PipelineDeTeste = app =>
            app.Run(contexto =>
            {
                contexto.Response.StatusCode = 200;
                contexto.Response.ContentType = "text/plain";
                return contexto.Response.WriteAsync("ok");
            });

        /// <summary>Ephemeral port livre (janela pequena de corrida até o ApiHost bindar; aceitável em teste local).</summary>
        private static int ObterPortaLivre()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int porta = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return porta;
        }

        [Fact]
        public async Task Start_PortaLivre_RespondeStatus200NumGetDeTeste()
        {
            var host = new ApiHost(new Autofac.ContainerBuilder().Build());
            int porta = ObterPortaLivre();

            try
            {
                host.Start(porta, PipelineDeTeste);
                Assert.Equal(EstadoApiHost.Ativa, host.Estado);
                Assert.Null(host.UltimoErro);
                Assert.Equal($"http://localhost:{porta}/", host.EnderecoBase);

                using (var client = new HttpClient())
                {
                    HttpResponseMessage resposta = await client.GetAsync($"http://localhost:{porta}/");

                    Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
                    Assert.Equal("ok", await resposta.Content.ReadAsStringAsync());
                }
            }
            finally
            {
                host.Stop();
            }
        }

        [Fact]
        public void Start_PortaOcupada_NaoLancaEEstadoReportaInativaPortaEmUso()
        {
            int porta = ObterPortaLivre();
            var listenerOcupando = new HttpListener();
            listenerOcupando.Prefixes.Add($"http://localhost:{porta}/");
            listenerOcupando.Start();

            try
            {
                var host = new ApiHost(new Autofac.ContainerBuilder().Build());

                var excecao = Record.Exception(() => host.Start(porta, PipelineDeTeste));

                Assert.Null(excecao);
                Assert.Equal(EstadoApiHost.InativaPortaEmUso, host.Estado);
                Assert.NotNull(host.UltimoErro);
                Assert.IsType<HttpListenerException>(host.UltimoErro);
                Assert.Null(host.EnderecoBase);
            }
            finally
            {
                listenerOcupando.Stop();
                listenerOcupando.Close();
            }
        }

        [Fact]
        public void Stop_LiberaAPorta_ConfirmadoReabrindoComOutroListener()
        {
            var host = new ApiHost(new Autofac.ContainerBuilder().Build());
            int porta = ObterPortaLivre();

            host.Start(porta, PipelineDeTeste);
            Assert.Equal(EstadoApiHost.Ativa, host.Estado);

            host.Stop();
            Assert.Equal(EstadoApiHost.Inativa, host.Estado);
            Assert.Null(host.EnderecoBase);

            // Confirma que o SO liberou a porta: reabrir aqui não pode lançar.
            var listenerDeConfirmacao = new HttpListener();
            listenerDeConfirmacao.Prefixes.Add($"http://localhost:{porta}/");
            var excecao = Record.Exception(() => listenerDeConfirmacao.Start());
            Assert.Null(excecao);

            listenerDeConfirmacao.Stop();
            listenerDeConfirmacao.Close();
        }

        /// <summary>
        /// Conecta o composition root Autofac (T-26) ao container que
        /// <see cref="ERPFinanceiro.Api.Startup"/>/<c>Autofac.WebApi2</c> usa (2ª parte do
        /// enunciado de T-27) — caminho de produção real via <see cref="ApiHost.Start(int)"/>
        /// (sem a sobrecarga de teste), contra o container de <see cref="CompositionRoot"/>.
        /// Nenhum controller existe ainda (T-30+), então a rota não mapeada responde 404 —
        /// o que já prova que o host subiu, o <c>AutofacWebApiDependencyResolver</c>
        /// resolveu com o container real (sem exceção) e nada derrubou o processo.
        /// </summary>
        [Fact]
        public async Task Start_SemSobrecargaDeTeste_UsaStartupComContainerRealDoCompositionRoot()
        {
            using (Autofac.IContainer container = CompositionRoot.Construir())
            {
                var host = new ApiHost(container);
                int porta = ObterPortaLivre();

                try
                {
                    host.Start(porta);
                    Assert.Equal(EstadoApiHost.Ativa, host.Estado);

                    using (var client = new HttpClient())
                    {
                        HttpResponseMessage resposta = await client.GetAsync(
                            $"http://localhost:{porta}/api/rota-inexistente-neste-lote");

                        // Web API sem controller mapeado ainda: 404, não uma exceção/derrubada
                        // do processo — prova que o pipeline (Startup + container real) subiu.
                        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
                    }
                }
                finally
                {
                    host.Stop();
                }
            }
        }
    }
}
