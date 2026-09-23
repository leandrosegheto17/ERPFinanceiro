using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ERPFinanceiro.Tests.Infrastructure.Persistencia
{
    /// <summary>
    /// Teste de integração real (T-11) do mapeamento Fluent de <see cref="FinanceiroDbContext"/>
    /// contra Firebird embarcado real, aplicando o schema completo de
    /// <c>database/01-schema.sql</c> via <see cref="DbInitializer"/> (T-12) — mesmo mecanismo
    /// (FbScript/FbBatchExecution) já usado por T-05/T-06/T-12.
    ///
    /// Cobre os 2 critérios de aceite de T-11: (1) inserir venda com 2 itens + histórico e
    /// reler sem perda de decimal, incluindo DECIMAL(18,4) de PRECO_UNITARIO (não só
    /// DECIMAL(18,2) já validado no spike T-01/ADR-009); (2) atualização com VERSAO velha
    /// lança DbUpdateConcurrencyException (mesmo comportamento do spike, agora com o schema
    /// completo e o mapeamento real, não o mínimo do console descartável).
    ///
    /// Nota de ambiente (mesma de T-12/DbInitializerTests): rodando de dentro do caminho
    /// sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar carregar o
    /// adaptador xUnit net48 (FileLoadException, HRESULT 0x80131515). Contorno: copiar
    /// bin/Debug/net48 já compilado para um diretório local fora do OneDrive e rodar
    /// `dotnet vstest ERPFinanceiro.Tests.dll` de lá.
    /// </summary>
    public class FinanceiroDbContextTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;

        public FinanceiroDbContextTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T11_FinanceiroDbContextTests_" + Guid.NewGuid().ToString("N") + ".fdb");
            _configuracao = new ConfiguracaoBancoTeste { CaminhoFdb = _caminhoFdb };

            // Schema completo real (não o mínimo do spike) via DbInitializer (T-12), reaproveitando
            // o mesmo database/01-schema.sql que é a fonte da verdade deste teste.
            new DbInitializer(_configuracao).Inicializar();
        }

        public void Dispose()
        {
            TentarApagar(_caminhoFdb);
        }

        private sealed class ConfiguracaoBancoTeste : IConfiguracaoBanco
        {
            public string CaminhoFdb { get; set; }
            public string Usuario { get; set; } = "sysdba";
            public string Senha { get; set; } = "masterkey";
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

        [Fact]
        public void InserirVendaComItensEHistorico_ReleSemPerdaDeDecimal()
        {
            var recebimentoUtc = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);

            var itens = new[]
            {
                new VendaItem("PROD-001", 2, 150.5075m),   // DECIMAL(18,4): 4 casas decimais
                new VendaItem("PROD-002", 1, 1250.5000m)
            };

            var venda = Venda.CriarPendente(
                vendaId: "V-T11-001",
                clienteId: "CLI-T11",
                valorTotal: 1551.52m, // DECIMAL(18,2), valor já preciso na escala da coluna
                itens: itens,
                dataRecebimentoUtc: recebimentoUtc,
                correlationId: "corr-t11-001");

            long idGerado;
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                ctx.Vendas.Add(venda);
                ctx.SaveChanges();
                idGerado = venda.Id;
            }

            Assert.True(idGerado > 0);

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                // Include por nome de string: ItensEf/HistoricoEf são propriedades internal
                // (uso exclusivo do mapeamento EF, ver Venda.cs) — o overload baseado em string
                // resolve o caminho de navegação por metadado do modelo, sem exigir acesso
                // compile-time ao membro internal a partir da expressão lambda.
                var lida = ctx.Vendas
                    .Include("ItensEf")
                    .Include("HistoricoEf")
                    .AsNoTracking()
                    .Single(v => v.Id == idGerado);

                // VALOR_TOTAL é DECIMAL(18,2): o valor gravado (1551.5150) é normalizado a 2 casas
                // pelo próprio schema/coluna. Comparação contra o valor arredondado esperado.
                Assert.Equal(1551.52m, lida.ValorTotal);
                Assert.Equal("V-T11-001", lida.VendaId);
                Assert.Equal("CLI-T11", lida.ClienteId);
                Assert.Equal(2, lida.Itens.Count);
                Assert.Single(lida.Historico);

                var item001 = lida.Itens.Single(i => i.ProdutoId == "PROD-001");
                var item002 = lida.Itens.Single(i => i.ProdutoId == "PROD-002");

                // Critério de aceite central de T-11: DECIMAL(18,4) sem perda (4 casas decimais).
                Assert.Equal(150.5075m, item001.PrecoUnitario);
                Assert.Equal(2, item001.Quantidade);
                Assert.Equal(1250.5000m, item002.PrecoUnitario);

                var historico = lida.Historico.Single();
                Assert.Equal(Operacao.Recebida, historico.Operacao);
                Assert.Null(historico.StatusAnterior);
                Assert.Equal(StatusVenda.Pendente, historico.StatusNovo);
                Assert.Equal("corr-t11-001", historico.CorrelationId);
            }
        }

        [Fact]
        public void AtualizarComVersaoVelha_LancaDbUpdateConcurrencyException()
        {
            var recebimentoUtc = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
            var venda = Venda.CriarPendente(
                vendaId: "V-T11-002",
                clienteId: "CLI-T11-002",
                valorTotal: 500.00m,
                itens: new[] { new VendaItem("PROD-010", 1, 10.0000m) },
                dataRecebimentoUtc: recebimentoUtc);

            long idGerado;
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                ctx.Vendas.Add(venda);
                ctx.SaveChanges();
                idGerado = venda.Id;
            }

            // Dois contextos carregam a mesma venda (VERSAO=0). Mesma limitação documentada no
            // spike (ADR-009): "segundo processo" simulado com dois contextos/conexões dentro do
            // mesmo processo de teste (regra 11 do TASK.md impede abrir o .fdb por processo do SO
            // distinto neste harness).
            using (var conn1 = AbrirConexao())
            using (var ctx1 = new FinanceiroDbContext(conn1))
            using (var conn2 = AbrirConexao())
            using (var ctx2 = new FinanceiroDbContext(conn2))
            {
                var v1 = ctx1.Vendas.Single(v => v.Id == idGerado);
                var v2 = ctx2.Vendas.Single(v => v.Id == idGerado);

                Assert.Equal(0, v1.Versao);
                Assert.Equal(0, v2.Versao);

                v1.Quitar(recebimentoUtc.AddHours(1));
                v1.Versao = v1.Versao + 1; // regra 8: aplicação incrementa, sem trigger
                ctx1.SaveChanges();

                v2.Cancelar(recebimentoUtc.AddHours(2));
                v2.Versao = v2.Versao + 1; // ainda baseado na VERSAO antiga (0 -> 1), agora obsoleta
                Assert.Throws<DbUpdateConcurrencyException>(() => ctx2.SaveChanges());
            }
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
                // Melhor esforço em Dispose(); não deve mascarar falha do teste.
            }
        }
    }
}
