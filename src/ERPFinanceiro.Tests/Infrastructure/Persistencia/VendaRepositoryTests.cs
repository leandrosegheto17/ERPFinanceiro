using System;
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
    /// Teste de integração real (T-14) de <see cref="VendaRepository"/> contra Firebird
    /// embarcado real, mesmo mecanismo de schema/conexão já usado por
    /// <see cref="FinanceiroDbContextTests"/> (T-11)/DbInitializer (T-12).
    ///
    /// Cobre os 3 critérios de aceite de T-14: (1) buscar venda existente por VendaId
    /// retorna com itens/histórico carregados; (2) buscar VendaId inexistente retorna
    /// null; (3) sem SQL concatenado (implementação usa só LINQ/EF, verificável por
    /// inspeção do código de produção — não há string SQL neste teste nem em
    /// VendaRepository.cs).
    ///
    /// Nota de ambiente (mesma de T-11/T-12): rodando de dentro do caminho sincronizado
    /// pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar carregar o adaptador
    /// xUnit net48 (FileLoadException, HRESULT 0x80131515). Contorno: copiar
    /// bin/Debug/net48 já compilado para um diretório local fora do OneDrive e rodar
    /// `dotnet vstest ERPFinanceiro.Tests.dll` de lá.
    /// </summary>
    public class VendaRepositoryTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;

        public VendaRepositoryTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T14_VendaRepositoryTests_" + Guid.NewGuid().ToString("N") + ".fdb");
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
        public void ObterPorVendaId_VendaExistente_RetornaComItensEHistorico()
        {
            var recebimentoUtc = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);
            var itens = new[]
            {
                new VendaItem("PROD-100", 3, 20.0000m),
                new VendaItem("PROD-200", 1, 99.9900m)
            };
            var venda = Venda.CriarPendente(
                vendaId: "V-T14-001",
                clienteId: "CLI-T14",
                valorTotal: 159.99m,
                itens: itens,
                dataRecebimentoUtc: recebimentoUtc,
                correlationId: "corr-t14-001");

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                repositorio.Adicionar(venda);
                ctx.SaveChanges();
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                var lida = repositorio.ObterPorVendaId("V-T14-001");

                Assert.NotNull(lida);
                Assert.Equal("V-T14-001", lida.VendaId);
                Assert.Equal("CLI-T14", lida.ClienteId);
                Assert.Equal(2, lida.Itens.Count);
                Assert.Single(lida.Historico);

                var item100 = lida.Itens.Single(i => i.ProdutoId == "PROD-100");
                Assert.Equal(3, item100.Quantidade);
                Assert.Equal(20.0000m, item100.PrecoUnitario);

                var historico = lida.Historico.Single();
                Assert.Equal(Operacao.Recebida, historico.Operacao);
                Assert.Equal(StatusVenda.Pendente, historico.StatusNovo);
                Assert.Equal("corr-t14-001", historico.CorrelationId);
            }
        }

        [Fact]
        public void ObterPorVendaId_VendaInexistente_RetornaNull()
        {
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                var lida = repositorio.ObterPorVendaId("V-NAO-EXISTE");

                Assert.Null(lida);
            }
        }

        [Fact]
        public void ObterParaConsultaSomenteLeitura_VendaExistente_RetornaSemTrackingComItensEHistorico()
        {
            var recebimentoUtc = new DateTime(2026, 9, 21, 11, 0, 0, DateTimeKind.Utc);
            var venda = Venda.CriarPendente(
                vendaId: "V-T14-002",
                clienteId: "CLI-T14-002",
                valorTotal: 10.00m,
                itens: new[] { new VendaItem("PROD-300", 1, 10.0000m) },
                dataRecebimentoUtc: recebimentoUtc);

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                repositorio.Adicionar(venda);
                ctx.SaveChanges();
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                var lida = repositorio.ObterParaConsultaSomenteLeitura("V-T14-002");

                Assert.NotNull(lida);
                Assert.Single(lida.Itens);
                Assert.Single(lida.Historico);
                // AsNoTracking: o contexto não está rastreando a entidade lida.
                Assert.Equal(System.Data.Entity.EntityState.Detached, ctx.Entry(lida).State);
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
