using System;
using System.IO;
using Autofac;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;

namespace T34SmokeHarness
{
    /// <summary>
    /// Harness descartável de T-34: sobe a API real (mesmo caminho de produção usado pelos
    /// testes de integração de T-30/T-31/T-32 — <see cref="ApiHost"/> + <see cref="Startup"/> +
    /// <see cref="CompositionRoot.Construir"/>) contra um Firebird embarcado real e novo,
    /// semeia as vendas conhecidas necessárias para os cenários 409/404 da coleção de fumaça
    /// (que não têm como ser produzidos só com os 3 endpoints do contrato v1.1, já que
    /// `POST /api/vendas` ainda não existe — T-62, Tier B condicional) e fica no ar até o
    /// arquivo `parar.txt` aparecer na pasta de saída, para os comandos curl de
    /// `docs/postman/` rodarem de fora contra ela.
    /// </summary>
    public static class Program
    {
        public static int Main()
        {
            var configuracao = new ConfiguracaoBancoAppConfig();

            ApagarFdbAnterior(configuracao.CaminhoFdb);

            Console.WriteLine("[T34] Inicializando banco (.fdb) via DbInitializer...");
            new DbInitializer(configuracao).Inicializar();

            Console.WriteLine("[T34] Semeando vendas conhecidas para os cenários 409/404 da coleção de fumaça...");
            SemearVendasConhecidas(configuracao);

            using (IContainer container = CompositionRoot.Construir())
            using (var host = new ApiHost(container))
            {
                int porta = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Api:Porta"]);
                host.Start(porta);

                if (host.Estado != EstadoApiHost.Ativa)
                {
                    Console.Error.WriteLine($"[T34] Falha ao subir a API. Estado: {host.Estado}, UltimoErro: {host.UltimoErro}");
                    return 1;
                }

                Console.WriteLine($"[T34] API ATIVA em http://localhost:{porta}/ — vendas semeadas:");
                Console.WriteLine("  V-T34-QUIT-NOVA        (inexistente, para o caminho feliz de quitacao)");
                Console.WriteLine("  V-T34-CANCELADA        (Cancelada, para 409 VENDA_JA_CANCELADA em quitacao)");
                Console.WriteLine("  V-T34-CANC-PEND        (Pendente, para o caminho feliz de cancelamento)");
                Console.WriteLine("  V-T34-CANC-QUIT        (Quitada, para 409 MOTIVO_OBRIGATORIO em cancelamento)");
                Console.WriteLine("  V-T34-NAO-EXISTE       (nunca criada, para 404 VENDA_NAO_ENCONTRADA em status)");
                Console.WriteLine("[T34] READY");

                string caminhoParar = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "parar.txt");
                TentarApagar(caminhoParar);
                while (!File.Exists(caminhoParar))
                {
                    System.Threading.Thread.Sleep(300);
                }

                Console.WriteLine("[T34] Sinal de parada recebido, encerrando...");
                host.Stop();
                TentarApagar(caminhoParar);
            }

            return 0;
        }

        private static void SemearVendasConhecidas(ERPFinanceiro.Infrastructure.Persistencia.ConfiguracaoBancoAppConfig configuracao)
        {
            using (var conn = AbrirConexao(configuracao))
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repo = new VendaRepository(ctx);
                var agora = DateTime.UtcNow.AddHours(-1);

                var cancelada = Venda.CriarCancelada("V-T34-CANCELADA", "Cliente desistiu (seed T-34)", agora);
                repo.Adicionar(cancelada);

                var pendente = Venda.CriarPendente(
                    "V-T34-CANC-PEND", "C-T34-01", 120.00m,
                    new[] { new VendaItem("P-T34-A", 1, 120.00m) }, agora);
                repo.Adicionar(pendente);

                var quitada = Venda.CriarPorQuitacao(
                    "V-T34-CANC-QUIT", "C-T34-02", 80.00m,
                    new[] { new VendaItem("P-T34-B", 1, 80.00m) }, agora, agora);
                repo.Adicionar(quitada);

                new UnitOfWork(ctx).SalvarAlteracoes();
            }
        }

        private static FbConnection AbrirConexao(ERPFinanceiro.Infrastructure.Persistencia.ConfiguracaoBancoAppConfig configuracao)
        {
            var csb = new FbConnectionStringBuilder
            {
                Database = configuracao.CaminhoFdb,
                UserID = configuracao.Usuario,
                Password = configuracao.Senha,
                ServerType = FbServerType.Embedded,
                Charset = "UTF8",
                Pooling = false
            };
            return new FbConnection(csb.ConnectionString);
        }

        private static void ApagarFdbAnterior(string caminho)
        {
            TentarApagar(caminho);
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
                // Melhor esforço.
            }
        }
    }
}
