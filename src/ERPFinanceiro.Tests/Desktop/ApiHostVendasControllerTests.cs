using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ERPFinanceiro.Api;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// Critério de aceite de T-30 (`POST /api/vendas/quitacao`): teste de integração real
    /// do pipeline HTTP completo, reaproveitando o padrão de <see cref="ApiHostTests"/>
    /// (T-27) — sobe <see cref="ApiHost"/>/<see cref="ERPFinanceiro.Api.Startup"/> reais
    /// contra o container real de <see cref="CompositionRoot"/> (agora com
    /// <c>VendasController</c> registrado, T-30) e faz requisições HTTP de fato contra
    /// Firebird embarcado real, não uma chamada em memória ao controller.
    /// <para>
    /// Usa o caminho de <c>.fdb</c> já reservado para testes do composition root real
    /// (`Banco:CaminhoFdb`/`Log:CaminhoArquivo` do `App.config` deste projeto, mesma nota de
    /// <see cref="CompositionRootTests"/>) — apagado e recriado no início deste teste para
    /// garantir estado limpo. Os 4 cenários do critério de aceite rodam sequencialmente
    /// num único <see cref="ApiHost"/>/container (evita abrir dois processos/contextos
    /// concorrentes no mesmo `.fdb`, regra 11 da Seção 1) — por isso um único <c>[Fact]</c>,
    /// não um por cenário.
    /// </para>
    /// <para>
    /// Nota de ambiente (mesma de T-11/T-12/T-14/T-15/T-18): rodando de dentro do caminho
    /// sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar carregar o
    /// adaptador xUnit net48 — contorno: copiar `bin/Debug/net48` já compilado para fora do
    /// OneDrive e rodar `dotnet vstest ERPFinanceiro.Tests.dll` de lá.
    /// </para>
    /// </summary>
    public class ApiHostVendasControllerTests : IDisposable
    {
        /// <summary>
        /// T-35: mesmo valor de <c>Api:ApiKey</c> do <c>App.config</c> local (gitignorado)
        /// de <c>ERPFinanceiro.Tests</c> — ver <c>App.config.example</c> do Desktop.
        /// </summary>
        private const string ChaveApiKeyDeTeste = "CHAVE_LOCAL_TESTE_T32";

        private readonly IConfiguracaoBanco _configuracao;

        public ApiHostVendasControllerTests()
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

        /// <summary>Ephemeral port livre (mesma estratégia de <see cref="ApiHostTests"/>).</summary>
        private static int ObterPortaLivre()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int porta = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return porta;
        }

        private FbConnection AbrirConexao()
        {
            var csb = new FbConnectionStringBuilder
            {
                Database = _configuracao.CaminhoFdb,
                UserID = _configuracao.Usuario,
                Password = _configuracao.Senha,
                ServerType = FbServerType.Embedded,
                Charset = "UTF8",
                Pooling = false
            };
            return new FbConnection(csb.ConnectionString);
        }

        /// <summary>Seed direto no `.fdb` real (mesma conexão que o host vai usar) de uma venda já Cancelada — cenário 409.</summary>
        private void SemearVendaCancelada(string vendaId)
        {
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarCancelada(vendaId, "Cliente desistiu", DateTime.UtcNow.AddHours(-1));
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }
        }

        private static StringContent JsonContent(string json)
        {
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        [Fact]
        public async Task PostQuitacao_PipelineHttpCompletoComContainerRealEFirebirdReal_CobreOsCenariosDoContratoV11()
        {
            const string vendaIdNova = "V-T30-HTTP-001";
            const string vendaIdCancelada = "V-T30-HTTP-CANC";

            SemearVendaCancelada(vendaIdCancelada);

            using (Autofac.IContainer container = CompositionRoot.Construir())
            using (var host = new ApiHost(container))
            {
                int porta = ObterPortaLivre();
                ERPFinanceiro.Api.Startup.ChaveApi = "chave-teste-t35";
                host.Start(porta);
                Assert.Equal(EstadoApiHost.Ativa, host.Estado);

                string baseUrl = $"http://localhost:{porta}/api/vendas/quitacao";

                using (var client = new HttpClient())
                {
                    // T-35: X-Api-Key obrigatório (ApiKeyHandler roda antes do roteamento).
                    client.DefaultRequestHeaders.Add(ApiKeyHandler.HeaderName, ChaveApiKeyDeTeste);

                    // Cenário 1: venda inexistente -> 200 {status:"Quitada",dataQuitacao}.
                    string corpoValido = $@"{{
                        ""vendaId"": ""{vendaIdNova}"",
                        ""clienteId"": ""C-T30"",
                        ""valorTotal"": 140.00,
                        ""itens"": [
                            {{ ""produtoId"": ""P-T30-A"", ""quantidade"": 2, ""precoUnitario"": 50.00 }},
                            {{ ""produtoId"": ""P-T30-B"", ""quantidade"": 1, ""precoUnitario"": 40.00 }}
                        ]
                    }}";

                    HttpResponseMessage resposta1 = await client.PostAsync(baseUrl, JsonContent(corpoValido));
                    Assert.Equal(HttpStatusCode.OK, resposta1.StatusCode);

                    JObject corpo1 = JObject.Parse(await resposta1.Content.ReadAsStringAsync());
                    Assert.Equal("Quitada", (string)corpo1["status"]);
                    var dataQuitacaoOriginal = (DateTime)corpo1["dataQuitacao"];

                    // Cenário 2: repetição do mesmo payload -> 200, mesma dataQuitacao (idempotência, P-2).
                    HttpResponseMessage resposta2 = await client.PostAsync(baseUrl, JsonContent(corpoValido));
                    Assert.Equal(HttpStatusCode.OK, resposta2.StatusCode);

                    JObject corpo2 = JObject.Parse(await resposta2.Content.ReadAsStringAsync());
                    Assert.Equal("Quitada", (string)corpo2["status"]);
                    // Tolerância de sub-milissegundo: Firebird TIMESTAMP arredonda para 0,1ms
                    // (menos precisão que o tick do .NET), então o round-trip JSON->banco->JSON
                    // pode diferir na última casa decimal sem que isso signifique um NOVO
                    // registro de dataQuitacao (idempotência real confirmada pelo histórico
                    // único em QuitacaoServiceTests, T-18) — mesma dataQuitacao dentro de 1ms.
                    Assert.True(
                        Math.Abs((dataQuitacaoOriginal - (DateTime)corpo2["dataQuitacao"]).TotalMilliseconds) < 1,
                        $"dataQuitacao da repetição ({(DateTime)corpo2["dataQuitacao"]:o}) deveria ser igual (idempotência, P-2) à original ({dataQuitacaoOriginal:o}).");

                    // Cenário 3: corpo inválido (itens vazio) -> 400, envelope {erro:{codigo,mensagem}}.
                    string corpoInvalido = @"{
                        ""vendaId"": ""V-T30-HTTP-INVALIDO"",
                        ""clienteId"": ""C-T30"",
                        ""valorTotal"": 0,
                        ""itens"": []
                    }";

                    HttpResponseMessage resposta3 = await client.PostAsync(baseUrl, JsonContent(corpoInvalido));
                    Assert.Equal(HttpStatusCode.BadRequest, resposta3.StatusCode);

                    JObject corpo3 = JObject.Parse(await resposta3.Content.ReadAsStringAsync());
                    Assert.Equal("PAYLOAD_INVALIDO", (string)corpo3["erro"]["codigo"]);
                    Assert.False(string.IsNullOrWhiteSpace((string)corpo3["erro"]["mensagem"]));

                    // Cenário 4: venda já cancelada -> 409 VENDA_JA_CANCELADA.
                    string corpoVendaCancelada = $@"{{
                        ""vendaId"": ""{vendaIdCancelada}"",
                        ""clienteId"": ""C-QUALQUER"",
                        ""valorTotal"": 10.00,
                        ""itens"": [ {{ ""produtoId"": ""P-X"", ""quantidade"": 1, ""precoUnitario"": 10.00 }} ]
                    }}";

                    HttpResponseMessage resposta4 = await client.PostAsync(baseUrl, JsonContent(corpoVendaCancelada));
                    Assert.Equal(HttpStatusCode.Conflict, resposta4.StatusCode);

                    JObject corpo4 = JObject.Parse(await resposta4.Content.ReadAsStringAsync());
                    Assert.Equal("VENDA_JA_CANCELADA", (string)corpo4["erro"]["codigo"]);
                }

                host.Stop();
            }
        }

        [Fact]
        public async Task PostCancelamento_PipelineHttpCompleto_CobreOsCenariosDoContratoV11()
        {
            SemearVenda("V-T32-PEND", quitar: false);
            SemearVenda("V-T32-QUIT", quitar: true);

            using (Autofac.IContainer container = CompositionRoot.Construir())
            using (var host = new ApiHost(container))
            {
                int porta = ObterPortaLivre();
                ERPFinanceiro.Api.Startup.ChaveApi = "chave-teste-t35";
                host.Start(porta);

                string url = $"http://localhost:{porta}/api/vendas/cancelamento";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", "chave-teste-t35");

                    // Pendente -> 200 Cancelada
                    var r1 = await client.PostAsync(url, JsonContent(@"{""vendaId"":""V-T32-PEND""}"));
                    Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
                    Assert.Equal("Cancelada", (string)JObject.Parse(await r1.Content.ReadAsStringAsync())["status"]);

                    // Repeticao -> 200 idempotente
                    var r2 = await client.PostAsync(url, JsonContent(@"{""vendaId"":""V-T32-PEND""}"));
                    Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
                    Assert.Equal("Cancelada", (string)JObject.Parse(await r2.Content.ReadAsStringAsync())["status"]);

                    // Desconhecida -> 200 (cria Cancelada)
                    var r3 = await client.PostAsync(url, JsonContent(@"{""vendaId"":""V-T32-DESC"",""motivo"":""x""}"));
                    Assert.Equal(HttpStatusCode.OK, r3.StatusCode);
                    Assert.Equal("Cancelada", (string)JObject.Parse(await r3.Content.ReadAsStringAsync())["status"]);

                    // Quitada sem motivo -> 409 MOTIVO_OBRIGATORIO
                    var r4 = await client.PostAsync(url, JsonContent(@"{""vendaId"":""V-T32-QUIT""}"));
                    Assert.Equal(HttpStatusCode.Conflict, r4.StatusCode);
                    Assert.Equal("MOTIVO_OBRIGATORIO", (string)JObject.Parse(await r4.Content.ReadAsStringAsync())["erro"]["codigo"]);

                    // Quitada com motivo -> 200
                    var r5 = await client.PostAsync(url, JsonContent(@"{""vendaId"":""V-T32-QUIT"",""motivo"":""Estorno""}"));
                    Assert.Equal(HttpStatusCode.OK, r5.StatusCode);

                    // Payload invalido -> 400 PAYLOAD_INVALIDO
                    var r6 = await client.PostAsync(url, JsonContent(@"{""motivo"":""x""}"));
                    Assert.Equal(HttpStatusCode.BadRequest, r6.StatusCode);
                    Assert.Equal("PAYLOAD_INVALIDO", (string)JObject.Parse(await r6.Content.ReadAsStringAsync())["erro"]["codigo"]);
                }

                host.Stop();
            }
        }

        [Fact]
        public async Task GetStatus_PipelineHttpCompletoComContainerRealEFirebirdReal_RetornaStatusPascalCaseE404()
        {
            const string vendaIdCancelada = "V-T31-HTTP-CANC";
            SemearVendaCancelada(vendaIdCancelada);

            using (Autofac.IContainer container = CompositionRoot.Construir())
            using (var host = new ApiHost(container))
            {
                int porta = ObterPortaLivre();
                ERPFinanceiro.Api.Startup.ChaveApi = "chave-teste-t35";
                host.Start(porta);
                Assert.Equal(EstadoApiHost.Ativa, host.Estado);

                string baseUrl = $"http://localhost:{porta}/api/vendas";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", "chave-teste-t35");

                    HttpResponseMessage r1 = await client.GetAsync($"{baseUrl}/{vendaIdCancelada}/status");
                    Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
                    JObject c1 = JObject.Parse(await r1.Content.ReadAsStringAsync());
                    Assert.Equal(vendaIdCancelada, (string)c1["vendaId"]);
                    Assert.Equal(JTokenType.String, c1["status"].Type);
                    Assert.Equal("Cancelada", (string)c1["status"]);

                    string corpoQuitacao = @"{ ""vendaId"": ""V-T31-HTTP-Q"", ""clienteId"": ""C-T31"", ""valorTotal"": 10.00,
                        ""itens"": [ { ""produtoId"": ""P-X"", ""quantidade"": 1, ""precoUnitario"": 10.00 } ] }";
                    HttpResponseMessage rq = await client.PostAsync($"{baseUrl}/quitacao", JsonContent(corpoQuitacao));
                    Assert.Equal(HttpStatusCode.OK, rq.StatusCode);

                    HttpResponseMessage r2 = await client.GetAsync($"{baseUrl}/V-T31-HTTP-Q/status");
                    Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
                    JObject c2 = JObject.Parse(await r2.Content.ReadAsStringAsync());
                    Assert.Equal("Quitada", (string)c2["status"]);

                    HttpResponseMessage r3 = await client.GetAsync($"{baseUrl}/V-T31-INEXISTENTE/status");
                    Assert.Equal(HttpStatusCode.NotFound, r3.StatusCode);
                    JObject c3 = JObject.Parse(await r3.Content.ReadAsStringAsync());
                    Assert.Equal("VENDA_NAO_ENCONTRADA", (string)c3["erro"]["codigo"]);
                }

                host.Stop();
            }
        }

        private void SemearVenda(string vendaId, bool quitar)
        {
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarPendente(
                    vendaId, "C-T32", 10m,
                    new[] { new VendaItem("P-T32", 1, 10m) },
                    DateTime.UtcNow.AddHours(-2));
                if (quitar)
                {
                    venda.Quitar(DateTime.UtcNow.AddHours(-1));
                }
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }
        }
    }
}
