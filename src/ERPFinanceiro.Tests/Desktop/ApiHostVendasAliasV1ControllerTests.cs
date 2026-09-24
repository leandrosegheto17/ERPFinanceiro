using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
    /// Critério de aceite de T-33 (alias <c>/api/v1/vendas/...</c>, D-05/P-8): teste de
    /// integração real do pipeline HTTP completo, mesmo padrão de
    /// <see cref="ApiHostVendasStatusControllerTests"/> (T-31) — sobe <see cref="ApiHost"/>/
    /// <see cref="ERPFinanceiro.Api.Startup"/> reais contra o container real de
    /// <see cref="CompositionRoot"/> e faz requisições HTTP de fato contra Firebird
    /// embarcado real.
    /// <para>
    /// Escopo do teste (documentado, como o critério de aceite de T-33 permite): não
    /// exercita os 3 endpoints x 2 rotas de forma exaustiva — isso já é coberto pelo
    /// caminho feliz de cada endpoint nos testes de T-30/T-31/T-32 (que usam a base
    /// <c>/api/vendas/...</c>). Este teste comprova especificamente que o **alias**
    /// funciona e responde de forma idêntica ao endpoint original, usando
    /// <c>GET .../status</c> (o mais simples/idempotente dos 3) contra a mesma venda
    /// semeada, nas duas bases (<c>/api/vendas/{vendaId}/status</c> e
    /// <c>/api/v1/vendas/{vendaId}/status</c>), e compara corpo e status HTTP.
    /// </para>
    /// </summary>
    public class ApiHostVendasAliasV1ControllerTests : IDisposable
    {
        /// <summary>
        /// T-35: mesmo valor de <c>Api:ApiKey</c> do <c>App.config</c> local (gitignorado)
        /// de <c>ERPFinanceiro.Tests</c> — ver <c>App.config.example</c> do Desktop.
        /// </summary>
        private const string ChaveApiKeyDeTeste = "CHAVE_LOCAL_TESTE_T32";

        private readonly IConfiguracaoBanco _configuracao;

        public ApiHostVendasAliasV1ControllerTests()
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
                var itens = new List<VendaItem> { new VendaItem("P-T33", 1, 25.00m) };
                var venda = Venda.CriarPendente(vendaId, "C-T33", 25.00m, itens, DateTime.UtcNow.AddHours(-1));
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }
        }

        [Fact]
        public async Task GetStatus_NasDuasRotasBaseEAliasV1_RespondemIdenticoParaAMesmaVenda()
        {
            const string vendaId = "V-T33-ALIAS-001";

            SemearVendaPendente(vendaId);

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

                    // Rota base do contrato v1.1 (T-31): /api/vendas/{vendaId}/status.
                    HttpResponseMessage respostaBase = await client.GetAsync(
                        $"http://localhost:{porta}/api/vendas/{vendaId}/status");
                    Assert.Equal(HttpStatusCode.OK, respostaBase.StatusCode);
                    string corpoBase = await respostaBase.Content.ReadAsStringAsync();
                    JObject jsonBase = JObject.Parse(corpoBase);

                    // Alias D-05/P-8 (T-33): /api/v1/vendas/{vendaId}/status.
                    HttpResponseMessage respostaAlias = await client.GetAsync(
                        $"http://localhost:{porta}/api/v1/vendas/{vendaId}/status");
                    Assert.Equal(HttpStatusCode.OK, respostaAlias.StatusCode);
                    string corpoAlias = await respostaAlias.Content.ReadAsStringAsync();
                    JObject jsonAlias = JObject.Parse(corpoAlias);

                    // Mesmo status HTTP e mesmo corpo JSON nas duas bases, para a mesma venda.
                    Assert.True(JToken.DeepEquals(jsonBase, jsonAlias));
                    Assert.Equal(vendaId, (string)jsonAlias["vendaId"]);
                    Assert.Equal("Pendente", (string)jsonAlias["status"]);
                }

                host.Stop();
            }
        }
    }
}
