using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using ERPFinanceiro.Api;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// Critério de aceite de T-31 (`GET /api/vendas/{vendaId}/status`): teste de
    /// integração real do pipeline HTTP completo, mesmo padrão de
    /// <see cref="ApiHostVendasControllerTests"/> (T-30) — sobe <see cref="ApiHost"/>/
    /// <see cref="ERPFinanceiro.Api.Startup"/> reais contra o container real de
    /// <see cref="CompositionRoot"/> (agora com <c>ConsultaService</c> injetado no
    /// <c>VendasController</c>, T-31) e faz requisições HTTP de fato contra Firebird
    /// embarcado real. Cobre os 2 cenários do critério de aceite: venda existente
    /// (200, <c>status</c> PascalCase) e venda inexistente (404 VENDA_NAO_ENCONTRADA).
    /// <para>
    /// Mesma nota de ambiente de T-11/T-12/T-14/T-15/T-18/T-30: rodando de dentro do
    /// caminho sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar
    /// carregar o adaptador xUnit net48 — contorno: copiar `bin/Debug/net48` já
    /// compilado para fora do OneDrive e rodar `dotnet vstest ERPFinanceiro.Tests.dll`
    /// de lá.
    /// </para>
    /// </summary>
    public class ApiHostVendasStatusControllerTests : IDisposable
    {
        /// <summary>
        /// T-35: mesmo valor de <c>Api:ApiKey</c> do <c>App.config</c> local (gitignorado)
        /// de <c>ERPFinanceiro.Tests</c> — ver <c>App.config.example</c> do Desktop.
        /// </summary>
        private const string ChaveApiKeyDeTeste = "CHAVE_LOCAL_TESTE_T32";

        private readonly IConfiguracaoBanco _configuracao;

        public ApiHostVendasStatusControllerTests()
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

        /// <summary>Seed direto no `.fdb` real de uma venda Pendente — cenário 200.</summary>
        private void SemearVendaPendente(string vendaId)
        {
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var itens = new List<VendaItem> { new VendaItem("P-T31", 1, 25.00m) };
                var venda = Venda.CriarPendente(vendaId, "C-T31", 25.00m, itens, DateTime.UtcNow.AddHours(-1));
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }
        }

        [Fact]
        public async Task GetStatus_PipelineHttpCompletoComContainerRealEFirebirdReal_CobreOsCenariosDoContratoV11()
        {
            const string vendaIdExistente = "V-T31-HTTP-001";
            const string vendaIdInexistente = "V-T31-HTTP-NAO-EXISTE";

            SemearVendaPendente(vendaIdExistente);

            using (Autofac.IContainer container = CompositionRoot.Construir())
            using (var host = new ApiHost(container))
            {
                int porta = ObterPortaLivre();
                host.Start(porta);
                Assert.Equal(EstadoApiHost.Ativa, host.Estado);

                using (var client = new HttpClient())
                {
                    // T-35: X-Api-Key obrigatório (ApiKeyHandler roda antes do roteamento).
                    client.DefaultRequestHeaders.Add(ApiKeyHandler.HeaderName, ChaveApiKeyDeTeste);

                    // Cenário 1: venda existente -> 200 {vendaId,status:"Pendente"}.
                    HttpResponseMessage resposta1 = await client.GetAsync(
                        $"http://localhost:{porta}/api/vendas/{vendaIdExistente}/status");
                    Assert.Equal(HttpStatusCode.OK, resposta1.StatusCode);

                    JObject corpo1 = JObject.Parse(await resposta1.Content.ReadAsStringAsync());
                    Assert.Equal(vendaIdExistente, (string)corpo1["vendaId"]);
                    Assert.Equal("Pendente", (string)corpo1["status"]);

                    // Cenário 2: venda inexistente -> 404 VENDA_NAO_ENCONTRADA.
                    HttpResponseMessage resposta2 = await client.GetAsync(
                        $"http://localhost:{porta}/api/vendas/{vendaIdInexistente}/status");
                    Assert.Equal(HttpStatusCode.NotFound, resposta2.StatusCode);

                    JObject corpo2 = JObject.Parse(await resposta2.Content.ReadAsStringAsync());
                    Assert.Equal("VENDA_NAO_ENCONTRADA", (string)corpo2["erro"]["codigo"]);
                    Assert.False(string.IsNullOrWhiteSpace((string)corpo2["erro"]["mensagem"]));
                }

                host.Stop();
            }
        }
    }
}
