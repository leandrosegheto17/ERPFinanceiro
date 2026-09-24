using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Infrastructure.Persistencia;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// Critério de aceite de T-36 (`GET /api/health`, `docs/contrato-v1.1.md` Seção
    /// 3.5, P-5): teste de integração real do pipeline HTTP completo, mesmo padrão de
    /// <see cref="ApiHostVendasStatusControllerTests"/> (T-31) — sobe <see cref="ApiHost"/>/
    /// <see cref="ERPFinanceiro.Api.Startup"/> reais contra o container real de
    /// <see cref="CompositionRoot"/> (agora com <see cref="ERPFinanceiro.Api.Controllers.HealthController"/>
    /// resolvido automaticamente por <c>RegisterApiControllers</c>, T-30) e faz
    /// requisições HTTP de fato contra Firebird embarcado real.
    /// <para>
    /// Cobre os 3 cenários do critério de aceite: (1) banco acessível -&gt; 200
    /// <c>{status:"ok",banco:"ok"}</c>; (2) banco inacessível (`.fdb` apagado depois
    /// de inicializado, mesmo princípio de <see cref="ERPFinanceiro.Tests.Infrastructure.Persistencia.HealthServiceTests"/>
    /// para simular falha de conexão sem lançar) -&gt; 503
    /// <c>{status:"degradado",banco:"falha"}</c>; (3) requisição sem nenhum header
    /// <c>X-Api-Key</c> responde normalmente (não 401) — nota: hoje (antes de T-35
    /// registrar o <c>ApiKeyHandler</c> no pipeline) toda requisição já passa sem
    /// autenticação de qualquer forma, então este cenário específico passa
    /// trivialmente nesta chamada; o que importa para T-36 é que o
    /// <see cref="ERPFinanceiro.Api.Controllers.HealthController"/> em si não lê nem
    /// exige nenhum header — quando T-35 isentar a rota <c>/api/health</c> no
    /// <c>ApiKeyHandler</c>, este mesmo teste continua passando sem qualquer mudança
    /// no controller.
    /// </para>
    /// <para>
    /// Mesma nota de ambiente de T-11/T-12/T-14/T-15/T-18/T-30/T-31: rodando de
    /// dentro do caminho sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest`
    /// pode recusar carregar o adaptador xUnit net48 — contorno: copiar
    /// `bin/Debug/net48` já compilado para fora do OneDrive e rodar
    /// `dotnet vstest ERPFinanceiro.Tests.dll` de lá.
    /// </para>
    /// </summary>
    public class ApiHostHealthControllerTests : IDisposable
    {
        private readonly IConfiguracaoBanco _configuracao;

        public ApiHostHealthControllerTests()
        {
            _configuracao = new ConfiguracaoBancoAppConfig();

            ApagarArquivosDeTesteAnteriores();

            new DbInitializer(_configuracao).Inicializar();
        }

        public void Dispose()
        {
            ApagarArquivosDeTesteAnteriores();
        }

        private void ApagarArquivosDeTesteAnteriores()
        {
            TentarApagar(_configuracao.CaminhoFdb);
        }

        private static void TentarApagar(string caminho)
        {
            try
            {
                if (File.Exists(caminho))
                {
                    File.Delete(caminho);
                }
            }
            catch
            {
                // Melhor esforço; não deve mascarar falha do teste.
            }
        }

        private static int ObterPortaLivre()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int porta = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return porta;
        }

        [Fact]
        public async Task GetHealth_PipelineHttpCompletoComContainerRealEFirebirdReal_CobreOsCenariosDoContratoV11()
        {
            using (Autofac.IContainer container = CompositionRoot.Construir())
            using (var host = new ApiHost(container))
            {
                int porta = ObterPortaLivre();
                host.Start(porta);
                Assert.Equal(EstadoApiHost.Ativa, host.Estado);

                using (var client = new HttpClient())
                {
                    // Cenário 1: banco acessível -> 200 {status:"ok",banco:"ok"}.
                    HttpResponseMessage resposta1 = await client.GetAsync(
                        $"http://localhost:{porta}/api/health");
                    Assert.Equal(HttpStatusCode.OK, resposta1.StatusCode);

                    JObject corpo1 = JObject.Parse(await resposta1.Content.ReadAsStringAsync());
                    Assert.Equal("ok", (string)corpo1["status"]);
                    Assert.Equal("ok", (string)corpo1["banco"]);

                    // Cenário 3 (checado aqui, junto do cenário 1, banco ainda ok): sem
                    // nenhum header X-Api-Key, a requisição responde normalmente (não
                    // 401) — já coberto pelo GetAsync acima, que não envia o header.
                    // Documentado explicitamente: hoje (sem ApiKeyHandler registrado,
                    // T-35 ainda em paralelo) qualquer rota responde sem autenticação;
                    // o ponto relevante para T-36 é que HealthController não lê/exige
                    // X-Api-Key por si mesmo.
                    Assert.False(resposta1.Headers.Contains("WWW-Authenticate"));

                    // Cenário 2: banco inacessível (.fdb apagado depois de inicializado,
                    // conexão Embedded subsequente falha dentro do driver Firebird,
                    // HealthService captura e traduz em ResultadoHealth.ComFalha, nunca
                    // lança) -> 503 {status:"degradado",banco:"falha"}.
                    TentarApagar(_configuracao.CaminhoFdb);

                    HttpResponseMessage resposta2 = await client.GetAsync(
                        $"http://localhost:{porta}/api/health");
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta2.StatusCode);

                    JObject corpo2 = JObject.Parse(await resposta2.Content.ReadAsStringAsync());
                    Assert.Equal("degradado", (string)corpo2["status"]);
                    Assert.Equal("falha", (string)corpo2["banco"]);
                }

                host.Stop();
            }
        }
    }
}
