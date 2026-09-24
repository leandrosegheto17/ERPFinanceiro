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
    /// Critério de aceite de T-32 (`POST /api/vendas/cancelamento`): teste de integração
    /// real do pipeline HTTP completo, mesmo padrão de <see cref="ApiHostVendasControllerTests"/>
    /// (T-30) e <see cref="ApiHostVendasStatusControllerTests"/> (T-31) — sobe
    /// <see cref="ApiHost"/>/<see cref="ERPFinanceiro.Api.Startup"/> reais contra o
    /// container real de <see cref="CompositionRoot"/> e faz requisições HTTP de fato
    /// contra Firebird embarcado real. Arquivo separado (em vez de reaproveitar
    /// <see cref="ApiHostVendasControllerTests"/>) para não colidir com a tarefa T-31,
    /// rodando em paralelo sobre o mesmo <c>VendasController</c>.
    /// <para>
    /// Cobre os cenários do critério de aceite de T-32: venda Pendente/desconhecida ->
    /// 200; venda Quitada sem <c>motivo</c> -> 409 <c>MOTIVO_OBRIGATORIO</c>; repetição
    /// (idempotência) -> 200. Um único <c>[Fact]</c> sequencial (regra 11 da Seção 1: não
    /// abrir dois processos/contextos concorrentes no mesmo <c>.fdb</c>).
    /// </para>
    /// <para>
    /// Nota de ambiente (mesma de T-11/T-12/T-14/T-15/T-18/T-30): rodando de dentro do
    /// caminho sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar
    /// carregar o adaptador xUnit net48 — contorno: copiar `bin/Debug/net48` já compilado
    /// para fora do OneDrive e rodar `dotnet vstest ERPFinanceiro.Tests.dll` de lá.
    /// </para>
    /// </summary>
    public class ApiHostVendasCancelamentoControllerTests : IDisposable
    {
        /// <summary>
        /// T-35: mesmo valor de <c>Api:ApiKey</c> do <c>App.config</c> local (gitignorado)
        /// de <c>ERPFinanceiro.Tests</c> — ver <c>App.config.example</c> do Desktop.
        /// </summary>
        private const string ChaveApiKeyDeTeste = "CHAVE_LOCAL_TESTE_T32";

        private readonly IConfiguracaoBanco _configuracao;

        public ApiHostVendasCancelamentoControllerTests()
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

        /// <summary>Seed direto no `.fdb` real de uma venda já Quitada — cenário 409 MOTIVO_OBRIGATORIO.</summary>
        private void SemearVendaQuitada(string vendaId)
        {
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var agora = DateTime.UtcNow.AddHours(-1);
                var itens = new[] { new VendaItem("P-CANC-01", 1, 99.90m) };
                var venda = Venda.CriarPorQuitacao(vendaId, "C-CANC", 99.90m, itens, agora, agora);
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }
        }

        private static StringContent JsonContent(string json)
        {
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        [Fact]
        public async Task PostCancelamento_PipelineHttpCompletoComContainerRealEFirebirdReal_CobreOsCenariosDoContratoV11()
        {
            const string vendaIdPendente = "V-T32-HTTP-PEND";
            const string vendaIdDesconhecida = "V-T32-HTTP-DESC";
            const string vendaIdQuitada = "V-T32-HTTP-QUIT";

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarPendente(vendaIdPendente, "C-T32", 55.00m,
                    new[] { new VendaItem("P-T32-A", 1, 55.00m) }, DateTime.UtcNow.AddHours(-1));
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }

            SemearVendaQuitada(vendaIdQuitada);

            using (Autofac.IContainer container = CompositionRoot.Construir())
            using (var host = new ApiHost(container))
            {
                int porta = ObterPortaLivre();
                host.Start(porta);
                Assert.Equal(EstadoApiHost.Ativa, host.Estado);

                string baseUrl = $"http://localhost:{porta}/api/vendas/cancelamento";

                using (var client = new HttpClient())
                {
                    // T-35: X-Api-Key obrigatório (ApiKeyHandler roda antes do roteamento).
                    client.DefaultRequestHeaders.Add(ApiKeyHandler.HeaderName, ChaveApiKeyDeTeste);

                    // Cenário 1: venda Pendente -> 200 {status:"Cancelada"}, motivo opcional (ausente).
                    string corpoPendente = $@"{{ ""vendaId"": ""{vendaIdPendente}"" }}";

                    HttpResponseMessage resposta1 = await client.PostAsync(baseUrl, JsonContent(corpoPendente));
                    Assert.Equal(HttpStatusCode.OK, resposta1.StatusCode);

                    JObject corpo1 = JObject.Parse(await resposta1.Content.ReadAsStringAsync());
                    Assert.Equal("Cancelada", (string)corpo1["status"]);

                    // Cenário 2: venda desconhecida -> 200 {status:"Cancelada"} (cria já Cancelada, D-08).
                    string corpoDesconhecida = $@"{{ ""vendaId"": ""{vendaIdDesconhecida}"", ""motivo"": ""Cliente desistiu"" }}";

                    HttpResponseMessage resposta2 = await client.PostAsync(baseUrl, JsonContent(corpoDesconhecida));
                    Assert.Equal(HttpStatusCode.OK, resposta2.StatusCode);

                    JObject corpo2 = JObject.Parse(await resposta2.Content.ReadAsStringAsync());
                    Assert.Equal("Cancelada", (string)corpo2["status"]);

                    // Cenário 3: venda Quitada sem motivo -> 409 MOTIVO_OBRIGATORIO, nada alterado.
                    string corpoQuitadaSemMotivo = $@"{{ ""vendaId"": ""{vendaIdQuitada}"" }}";

                    HttpResponseMessage resposta3 = await client.PostAsync(baseUrl, JsonContent(corpoQuitadaSemMotivo));
                    Assert.Equal(HttpStatusCode.Conflict, resposta3.StatusCode);

                    JObject corpo3 = JObject.Parse(await resposta3.Content.ReadAsStringAsync());
                    Assert.Equal("MOTIVO_OBRIGATORIO", (string)corpo3["erro"]["codigo"]);
                    Assert.False(string.IsNullOrWhiteSpace((string)corpo3["erro"]["mensagem"]));

                    // Cenário 3b: mesma venda Quitada, agora com motivo -> 200 {status:"Cancelada"}.
                    string corpoQuitadaComMotivo = $@"{{ ""vendaId"": ""{vendaIdQuitada}"", ""motivo"": ""Cliente desistiu da compra"" }}";

                    HttpResponseMessage resposta3b = await client.PostAsync(baseUrl, JsonContent(corpoQuitadaComMotivo));
                    Assert.Equal(HttpStatusCode.OK, resposta3b.StatusCode);

                    JObject corpo3b = JObject.Parse(await resposta3b.Content.ReadAsStringAsync());
                    Assert.Equal("Cancelada", (string)corpo3b["status"]);

                    // Cenário 4: repetição (idempotência, P-2) sobre a mesma venda já Cancelada -> 200.
                    HttpResponseMessage resposta4 = await client.PostAsync(baseUrl, JsonContent(corpoPendente));
                    Assert.Equal(HttpStatusCode.OK, resposta4.StatusCode);

                    JObject corpo4 = JObject.Parse(await resposta4.Content.ReadAsStringAsync());
                    Assert.Equal("Cancelada", (string)corpo4["status"]);

                    // Cenário 5: payload inválido (vendaId ausente) -> 400 PAYLOAD_INVALIDO.
                    string corpoInvalido = @"{ ""motivo"": ""sem vendaId"" }";

                    HttpResponseMessage resposta5 = await client.PostAsync(baseUrl, JsonContent(corpoInvalido));
                    Assert.Equal(HttpStatusCode.BadRequest, resposta5.StatusCode);

                    JObject corpo5 = JObject.Parse(await resposta5.Content.ReadAsStringAsync());
                    Assert.Equal("PAYLOAD_INVALIDO", (string)corpo5["erro"]["codigo"]);
                }

                host.Stop();
            }
        }
    }
}
