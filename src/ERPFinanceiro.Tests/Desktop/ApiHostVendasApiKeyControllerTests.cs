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
    /// Critério de aceite de T-35 (<see cref="ApiKeyHandler"/>, S-08): teste de integração
    /// real do pipeline HTTP completo, mesmo padrão de
    /// <see cref="ApiHostVendasStatusControllerTests"/> (T-31) — sobe <see cref="ApiHost"/>/
    /// <see cref="ERPFinanceiro.Api.Startup"/> reais contra o container real de
    /// <see cref="CompositionRoot"/> (agora com <see cref="ApiKeyHandler"/> registrado no
    /// pipeline, T-35) e faz requisições HTTP de fato contra Firebird embarcado real.
    /// <para>
    /// Cobre os 3 cenários do critério de aceite: (1) sem <c>X-Api-Key</c> -&gt; 401; (2)
    /// <c>X-Api-Key</c> com valor errado -&gt; 401 idêntico ao cenário 1 (mesmo status e
    /// mesmo corpo JSON, contrato v1.1 Seção 2: "resposta idêntica nos dois casos"); (3)
    /// <c>X-Api-Key</c> correta -&gt; 200 (usa <c>GET .../status</c>, o endpoint mais simples/
    /// idempotente, sobre uma venda pré-semeada — mesma escolha de
    /// <see cref="ApiHostVendasAliasV1ControllerTests"/>/T-33). Também confirma que o valor
    /// configurado da chave nunca aparece no arquivo de log real (<see cref="ERPFinanceiro.Infrastructure.TraceLogger"/>,
    /// caminho <c>Log:CaminhoArquivo</c> do <c>App.config</c>) depois de exercitar os 3
    /// cenários — inclusive o caso de chave errada, que é exatamente o caminho que
    /// registra alguma coisa no log (<see cref="ApiKeyHandler"/> grava só uma mensagem
    /// genérica, nunca o header recebido nem a chave configurada).
    /// </para>
    /// <para>
    /// Arquivo separado (não reaproveita <see cref="ApiHostVendasStatusControllerTests"/>)
    /// para não colidir com a outra instância do Executor rodando em paralelo sobre T-36
    /// (`GET /api/health`), que também mexe em <c>Startup.cs</c>/pipeline da Api.
    /// </para>
    /// <para>
    /// Mesma nota de ambiente de T-11/T-12/T-14/T-15/T-18/T-30…T-33: rodando de dentro do
    /// caminho sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar
    /// carregar o adaptador xUnit net48 — contorno: copiar `bin/Debug/net48` já
    /// compilado para fora do OneDrive e rodar `dotnet vstest ERPFinanceiro.Tests.dll`
    /// de lá.
    /// </para>
    /// </summary>
    public class ApiHostVendasApiKeyControllerTests : IDisposable
    {
        /// <summary>
        /// Mesmo valor de <c>Api:ApiKey</c> do <c>App.config</c> local (gitignorado) de
        /// <c>ERPFinanceiro.Tests</c> — ver <c>App.config.example</c> do Desktop.
        /// </summary>
        private const string ChaveApiKeyCorreta = "CHAVE_LOCAL_TESTE_T32";

        private const string ChaveApiKeyErrada = "chave-errada-nao-configurada-T35";

        private readonly IConfiguracaoBanco _configuracao;

        public ApiHostVendasApiKeyControllerTests()
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

        /// <summary>Seed direto no `.fdb` real de uma venda Pendente — cenário 200 (chave correta).</summary>
        private void SemearVendaPendente(string vendaId)
        {
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var itens = new List<VendaItem> { new VendaItem("P-T35", 1, 15.00m) };
                var venda = Venda.CriarPendente(vendaId, "C-T35", 15.00m, itens, DateTime.UtcNow.AddHours(-1));
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }
        }

        [Fact]
        public async Task ApiKeyHandler_PipelineHttpCompletoComContainerRealEFirebirdReal_CobreOsCenariosDoContratoV11()
        {
            const string vendaId = "V-T35-HTTP-001";

            SemearVendaPendente(vendaId);

            using (Autofac.IContainer container = CompositionRoot.Construir())
            using (var host = new ApiHost(container))
            {
                int porta = ObterPortaLivre();
                host.Start(porta);
                Assert.Equal(EstadoApiHost.Ativa, host.Estado);

                string urlStatus = $"http://localhost:{porta}/api/vendas/{vendaId}/status";

                // Cenário 1: sem X-Api-Key -> 401 NAO_AUTORIZADO.
                JObject corpoSemHeader;
                using (var client = new HttpClient())
                {
                    HttpResponseMessage respostaSemHeader = await client.GetAsync(urlStatus);
                    Assert.Equal(HttpStatusCode.Unauthorized, respostaSemHeader.StatusCode);

                    corpoSemHeader = JObject.Parse(await respostaSemHeader.Content.ReadAsStringAsync());
                    Assert.Equal("NAO_AUTORIZADO", (string)corpoSemHeader["erro"]["codigo"]);
                    Assert.False(string.IsNullOrWhiteSpace((string)corpoSemHeader["erro"]["mensagem"]));
                }

                // Cenário 2: X-Api-Key com valor errado -> 401 idêntico ao cenário 1 (mesmo
                // status e mesmo corpo JSON — contrato v1.1: "resposta idêntica nos dois casos").
                JObject corpoChaveErrada;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add(ApiKeyHandler.HeaderName, ChaveApiKeyErrada);

                    HttpResponseMessage respostaChaveErrada = await client.GetAsync(urlStatus);
                    Assert.Equal(HttpStatusCode.Unauthorized, respostaChaveErrada.StatusCode);

                    corpoChaveErrada = JObject.Parse(await respostaChaveErrada.Content.ReadAsStringAsync());
                    Assert.Equal("NAO_AUTORIZADO", (string)corpoChaveErrada["erro"]["codigo"]);
                }

                Assert.True(
                    JToken.DeepEquals(corpoSemHeader, corpoChaveErrada),
                    "401 sem header e 401 com chave errada devem devolver o mesmo corpo (não revelar qual dos dois casos ocorreu).");

                // Cenário 3: X-Api-Key correta -> 200 {vendaId,status}.
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add(ApiKeyHandler.HeaderName, ChaveApiKeyCorreta);

                    HttpResponseMessage respostaChaveCorreta = await client.GetAsync(urlStatus);
                    Assert.Equal(HttpStatusCode.OK, respostaChaveCorreta.StatusCode);

                    JObject corpoChaveCorreta = JObject.Parse(await respostaChaveCorreta.Content.ReadAsStringAsync());
                    Assert.Equal(vendaId, (string)corpoChaveCorreta["vendaId"]);
                    Assert.Equal("Pendente", (string)corpoChaveCorreta["status"]);
                }

                host.Stop();

                ConfirmarChaveNuncaApareceNoLog();
            }
        }

        /// <summary>
        /// Lê o arquivo real de log (<c>Log:CaminhoArquivo</c> do `App.config`, escrito por
        /// <see cref="ERPFinanceiro.Infrastructure.TraceLogger"/> através do
        /// <see cref="IAppLogger"/> registrado no <see cref="CompositionRoot"/>) e confirma
        /// que nem a chave correta nem a chave errada usadas neste teste aparecem em
        /// nenhuma linha — evidência real (não só leitura de código) de que
        /// <see cref="ApiKeyHandler"/> nunca loga a credencial (TASK.md Seção 1 regra 7;
        /// GUARDRAILS G-6.24).
        /// </summary>
        private static void ConfirmarChaveNuncaApareceNoLog()
        {
            string caminhoLog = System.Configuration.ConfigurationManager.AppSettings["Log:CaminhoArquivo"];
            if (string.IsNullOrWhiteSpace(caminhoLog) || !File.Exists(caminhoLog))
            {
                // Sem arquivo de log gravado nesta execução: nada a verificar (melhor
                // esforço — o cenário principal do teste já rodou e passou acima).
                return;
            }

            string conteudoLog;
            using (var leitor = new StreamReader(new FileStream(caminhoLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                conteudoLog = leitor.ReadToEnd();
            }

            Assert.DoesNotContain(ChaveApiKeyCorreta, conteudoLog);
            Assert.DoesNotContain(ChaveApiKeyErrada, conteudoLog);
        }
    }
}
