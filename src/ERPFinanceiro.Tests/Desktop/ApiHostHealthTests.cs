using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Autofac;
using Autofac.Integration.WebApi;
using ERPFinanceiro.Api.Controllers;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// Critério de aceite de T-36: <c>GET /api/health</c> via HTTP real (ApiHost/OWIN) com
    /// <see cref="IHealthService"/> simulado (200 banco ok; 503 banco com falha), sem
    /// <c>X-Api-Key</c>, e sem vazar <c>ResultadoHealth.Mensagem</c> no corpo.
    /// </summary>
    public class ApiHostHealthTests
    {
        private static int PortaLivre()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int p = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return p;
        }

        private static async Task<(HttpStatusCode, JObject, string)> Chamar(ResultadoHealth resultado)
        {
            var health = new Mock<IHealthService>();
            health.Setup(h => h.ObterStatus()).Returns(resultado);

            var builder = new ContainerBuilder();
            builder.RegisterInstance(health.Object).As<IHealthService>();
            builder.RegisterApiControllers(typeof(HealthController).Assembly);

            using (IContainer container = builder.Build())
            using (var host = new ApiHost(container))
            {
                int porta = PortaLivre();
                host.Start(porta);
                using (var client = new HttpClient())
                {
                    HttpResponseMessage r = await client.GetAsync($"http://localhost:{porta}/api/health");
                    string corpo = await r.Content.ReadAsStringAsync();
                    return (r.StatusCode, JObject.Parse(corpo), corpo);
                }
            }
        }

        [Fact]
        public async Task Get_BancoOk_Retorna200SemApiKey()
        {
            var (status, json, _) = await Chamar(ResultadoHealth.ComSucesso());

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal("ok", (string)json["status"]);
            Assert.Equal("ok", (string)json["banco"]);
        }

        [Fact]
        public async Task Get_BancoInacessivel_Retorna503SemVazarMensagem()
        {
            var (status, json, corpo) = await Chamar(ResultadoHealth.ComFalha(@"C:\segredo\banco.fdb inacessivel"));

            Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
            Assert.Equal("degradado", (string)json["status"]);
            Assert.Equal("falha", (string)json["banco"]);
            Assert.DoesNotContain("segredo", corpo);
        }
    }
}
