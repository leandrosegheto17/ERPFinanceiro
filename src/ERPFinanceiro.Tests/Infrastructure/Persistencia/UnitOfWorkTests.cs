using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using ERPFinanceiro.Application.Exceptions;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ERPFinanceiro.Tests.Infrastructure.Persistencia
{
    /// <summary>
    /// Teste de integração real (T-15) de <see cref="UnitOfWork"/> contra Firebird
    /// embarcado real, mesmo mecanismo de schema/conexão já usado por
    /// <see cref="FinanceiroDbContextTests"/> (T-11)/<see cref="VendaRepositoryTests"/> (T-14).
    ///
    /// Cobre os 2 critérios de aceite de T-15: (1) falha injetada no histórico (um valor de
    /// <c>Operacao</c> fora de 0/1/2 — CHECK CK_FIN_VENDA_HIST_OPER, database/01-schema.sql
    /// — força o Firebird a rejeitar o INSERT desse registro no servidor, não no cliente)
    /// faz rollback da venda inteira: nada persiste, nem o registro pai (confirma que o EF6
    /// já agrupa Venda+itens+histórico numa única transação implícita por SaveChanges, sem
    /// precisar de TransactionScope manual); (2) conflito de VERSAO e violação de
    /// UNIQUE(VENDA_ID) viram ConcorrenciaException.
    ///
    /// Nota de ambiente (mesma de T-11/T-12/T-14): rodando de dentro do caminho sincronizado
    /// pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar carregar o adaptador xUnit
    /// net48 (FileLoadException, HRESULT 0x80131515). Contorno: copiar bin/Debug/net48 já
    /// compilado para um diretório local fora do OneDrive e rodar
    /// `dotnet vstest ERPFinanceiro.Tests.dll` de lá.
    /// </summary>
    public class UnitOfWorkTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;

        public UnitOfWorkTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T15_UnitOfWorkTests_" + Guid.NewGuid().ToString("N") + ".fdb");
            _configuracao = new ConfiguracaoBancoTeste { CaminhoFdb = _caminhoFdb };

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
        public void SalvarAlteracoes_FalhaInjetadaNoHistorico_FazRollbackDaVendaInteira()
        {
            var recebimentoUtc = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);
            var venda = Venda.CriarPendente(
                vendaId: "V-T15-ROLLBACK",
                clienteId: "CLI-T15",
                valorTotal: 100.00m,
                itens: new[] { new VendaItem("PROD-T15", 1, 100.0000m) },
                dataRecebimentoUtc: recebimentoUtc,
                correlationId: "corr-t15-rollback");

            // Falha injetada: um segundo item de histórico com Operacao fora do domínio
            // 0/1/2 (CK_FIN_VENDA_HIST_OPER, database/01-schema.sql) força o Firebird a
            // rejeitar o INSERT desse registro no meio do mesmo SaveChanges() que também
            // grava a venda e o item — sem validação client-side do EF6 no caminho (não há
            // HasMaxLength/Required equivalente para o range de um enum mapeado como
            // SMALLINT), então a falha chega mesmo ao servidor. VendaHistorico não valida a
            // Operacao no Domain (é um enum comum, cast arbitrário é possível em C#) e seu
            // construtor real é internal sem overload de teste — criado via reflexão para não
            // introduzir uma subclasse (quebraria o mapeamento EF6, que não tem TPH
            // configurado para VendaHistorico).
            venda.HistoricoEf.Add(CriarHistoricoComOperacaoInvalida());

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                var unitOfWork = new UnitOfWork(ctx);
                repositorio.Adicionar(venda);

                Assert.ThrowsAny<DbUpdateException>(() => unitOfWork.SalvarAlteracoes());
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                bool vendaPersistida = ctx.Vendas.Any(v => v.VendaId == "V-T15-ROLLBACK");
                bool itemPersistido = ctx.VendaItens.Any(i => i.ProdutoId == "PROD-T15");
                bool historicoPersistido = ctx.VendaHistoricos.AsNoTracking().Any();

                Assert.False(vendaPersistida, "Venda não deveria persistir: rollback da transação inteira.");
                Assert.False(itemPersistido, "Item não deveria persistir: rollback da transação inteira.");
                Assert.False(historicoPersistido, "Histórico não deveria persistir: rollback da transação inteira.");
            }
        }

        [Fact]
        public void SalvarAlteracoes_ConflitoDeVersao_LancaConcorrenciaException()
        {
            var recebimentoUtc = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);
            var venda = Venda.CriarPendente(
                vendaId: "V-T15-VERSAO",
                clienteId: "CLI-T15-VERSAO",
                valorTotal: 500.00m,
                itens: new[] { new VendaItem("PROD-T15-V", 1, 10.0000m) },
                dataRecebimentoUtc: recebimentoUtc);

            long idGerado;
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                var unitOfWork = new UnitOfWork(ctx);
                repositorio.Adicionar(venda);
                unitOfWork.SalvarAlteracoes();
                idGerado = venda.Id;
            }

            // Mesma limitação documentada no spike (ADR-009)/T-11: "segundo processo"
            // simulado com dois contextos/conexões dentro do mesmo processo de teste.
            using (var conn1 = AbrirConexao())
            using (var ctx1 = new FinanceiroDbContext(conn1))
            using (var conn2 = AbrirConexao())
            using (var ctx2 = new FinanceiroDbContext(conn2))
            {
                var v1 = ctx1.Vendas.Single(v => v.Id == idGerado);
                var v2 = ctx2.Vendas.Single(v => v.Id == idGerado);

                v1.Quitar(recebimentoUtc.AddHours(1));
                v1.Versao = v1.Versao + 1; // regra 8: aplicação incrementa, sem trigger
                new UnitOfWork(ctx1).SalvarAlteracoes();

                v2.Cancelar(recebimentoUtc.AddHours(2));
                v2.Versao = v2.Versao + 1; // ainda baseado na VERSAO antiga (0 -> 1), agora obsoleta

                Assert.Throws<ConcorrenciaException>(() => new UnitOfWork(ctx2).SalvarAlteracoes());
            }
        }

        [Fact]
        public void SalvarAlteracoes_ViolacaoDeUniqueVendaId_LancaConcorrenciaException()
        {
            var recebimentoUtc = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda1 = Venda.CriarPendente(
                    vendaId: "V-T15-DUP",
                    clienteId: "CLI-T15-DUP-1",
                    valorTotal: 10.00m,
                    itens: new[] { new VendaItem("PROD-DUP-1", 1, 10.0000m) },
                    dataRecebimentoUtc: recebimentoUtc);

                new VendaRepository(ctx).Adicionar(venda1);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                // Mesmo VendaId (identificador de negócio) — viola UNIQUE(VENDA_ID)
                // (UQ_FIN_VENDA_VENDA_ID, database/01-schema.sql), simulando duas operações
                // concorrentes tentando criar a mesma venda nova (upsert RN-06, T-18).
                var venda2 = Venda.CriarPendente(
                    vendaId: "V-T15-DUP",
                    clienteId: "CLI-T15-DUP-2",
                    valorTotal: 20.00m,
                    itens: new[] { new VendaItem("PROD-DUP-2", 1, 20.0000m) },
                    dataRecebimentoUtc: recebimentoUtc);

                new VendaRepository(ctx).Adicionar(venda2);

                Assert.Throws<ConcorrenciaException>(() => new UnitOfWork(ctx).SalvarAlteracoes());
            }
        }

        /// <summary>
        /// Cria um <see cref="VendaHistorico"/> real (não subclasse — evitaria o
        /// mapeamento EF6, sem TPH configurado) com <c>Operacao</c> fora do domínio válido
        /// (0/1/2, CK_FIN_VENDA_HIST_OPER), via reflexão sobre o construtor <c>internal</c>
        /// — o único disponível hoje não tem overload de teste, e o Domain não valida o
        /// range do enum (cast explícito de um int arbitrário é permitido pela linguagem).
        /// </summary>
        private static VendaHistorico CriarHistoricoComOperacaoInvalida()
        {
            var construtor = typeof(VendaHistorico).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(Operacao), typeof(StatusVenda?), typeof(StatusVenda), typeof(DateTime), typeof(string), typeof(string) },
                null);

            return (VendaHistorico)construtor.Invoke(new object[]
            {
                (Operacao)99, // fora de 0 (Recebida)/1 (Quitacao)/2 (Cancelamento)
                (StatusVenda?)StatusVenda.Pendente,
                StatusVenda.Cancelada,
                new DateTime(2026, 9, 21, 11, 0, 0, DateTimeKind.Utc),
                "motivo de teste",
                null
            });
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
