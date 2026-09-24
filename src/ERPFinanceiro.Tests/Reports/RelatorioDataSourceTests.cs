using System;
using System.IO;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Infrastructure.Persistencia;
using ERPFinanceiro.Reports;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ERPFinanceiro.Tests.Reports
{
    /// <summary>Totais por status (T-59, S-07/CA-07.3) com repositório mockado, sem Firebird.</summary>
    public class RelatorioDataSourceTotaisTests
    {
        private static readonly DateTime D = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

        private static RelatorioDataSource Fonte()
        {
            var v1001 = Venda.CriarPorQuitacao("V-1001", "CLI-001", 500.50m,
                new[] { new VendaItem("P1", 2, 150.0000m) }, D, D.AddDays(2));
            var v1002 = Venda.CriarPendente("V-1002", "CLI-002", 450.50m,
                new[] { new VendaItem("P3", 3, 100.0000m) }, D.AddDays(4));
            var v1003 = Venda.CriarCancelada("V-1003", "desconhecida", D.AddDays(6));

            var leitura = new Moq.Mock<IVendaConsultaLeitura>();
            leitura.Setup(l => l.ListarTodas())
                .Returns(new System.Collections.Generic.List<Venda> { v1003, v1002, v1001 });
            return new RelatorioDataSource(new ConsultaService(new Moq.Mock<IVendaRepository>().Object, leitura.Object));
        }

        [Fact]
        public void ObterTotais_ComSeed_SomaPorStatusConfereComTotalListado()
        {
            var t = Fonte().ObterTotais(new FiltroVendas());

            Assert.Equal(500.50m, t.Quitado.Valor);
            Assert.Equal(1, t.Quitado.Quantidade);
            Assert.Equal(0m, t.Cancelado.Valor); // nulo = 0
            Assert.Equal(1, t.Cancelado.Quantidade);
            Assert.Equal(450.50m, t.Pendente.Valor);
            Assert.Equal(951.00m, t.TotalListado);
            Assert.Equal(t.TotalListado, t.Quitado.Valor + t.Cancelado.Valor + t.Pendente.Valor);
        }

        [Fact]
        public void ObterTotais_D03Rejeitada_OmitePendente()
        {
            var t = Fonte().ObterTotais(new FiltroVendas(), incluirPendente: false);

            Assert.Null(t.Pendente);
            Assert.Equal(500.50m, t.Quitado.Valor);
        }
    }

    /// <summary>
    /// Teste de integração real (T-48) de <see cref="RelatorioDataSource"/> contra
    /// Firebird embarcado real, mesmo mecanismo de schema/conexão já usado por
    /// <see cref="ERPFinanceiro.Tests.Infrastructure.Persistencia.ConsultaServiceListarTests"/>
    /// (T-23). Cobre os 2 critérios de aceite de T-48: (1) com massa equivalente ao seed
    /// de T-06 (3 vendas), "Total listado" = 951,00 (CA-07.3); (2) lista vazia -> total
    /// 0,00.
    ///
    /// Nota de ambiente (mesma de T-11/T-12/T-14/T-23): rodando de dentro do caminho
    /// sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar carregar o
    /// adaptador xUnit net48 (FileLoadException, HRESULT 0x80131515). Contorno: copiar
    /// bin/Debug/net48 já compilado para um diretório local fora do OneDrive e rodar
    /// `dotnet vstest ERPFinanceiro.Tests.dll` de lá.
    /// </summary>
    public class RelatorioDataSourceTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;

        public RelatorioDataSourceTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T48_RelatorioDataSourceTests_" + Guid.NewGuid().ToString("N") + ".fdb");
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
        public void Obter_ComMassaEquivalenteAoSeedT06_TotalListadoIgualA951()
        {
            // Mesma composição de database/02-seed.sql (T-06, CA-07.3):
            // V-1001 Quitada 500.50 + V-1002 Pendente 450.50 + V-1003 Cancelada (sem itens,
            // ValorTotal nulo, ADR-007/D-08) = 951.00.
            var v1001 = Venda.CriarPorQuitacao(
                vendaId: "V-1001",
                clienteId: "CLI-001",
                valorTotal: 500.50m,
                itens: new[] { new VendaItem("PROD-001", 2, 150.0000m), new VendaItem("PROD-002", 1, 200.5000m) },
                dataRecebimentoUtc: new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
                dataQuitacaoUtc: new DateTime(2026, 9, 3, 14, 30, 0, DateTimeKind.Utc));

            var v1002 = Venda.CriarPendente(
                vendaId: "V-1002",
                clienteId: "CLI-002",
                valorTotal: 450.50m,
                itens: new[] { new VendaItem("PROD-003", 3, 100.0000m), new VendaItem("PROD-004", 2, 75.2500m) },
                dataRecebimentoUtc: new DateTime(2026, 9, 5, 9, 15, 0, DateTimeKind.Utc));

            var v1003 = Venda.CriarCancelada(
                vendaId: "V-1003",
                motivo: "Venda nao localizada no sistema de origem (D-08)",
                dataCancelamentoUtc: new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc));

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                repositorio.Adicionar(v1001);
                repositorio.Adicionar(v1002);
                repositorio.Adicionar(v1003);
                ctx.SaveChanges();
            }

            ResultadoRelatorioVendas resultado;
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                var consultaService = new ConsultaService(repositorio, repositorio);
                var dataSource = new RelatorioDataSource(consultaService);

                resultado = dataSource.Obter(new FiltroVendas());
            }

            Assert.Equal(3, resultado.Linhas.Count);
            Assert.Equal(951.00m, resultado.TotalListado);
            Assert.Equal(1, resultado.QuantidadeSemValor);

            // Ordenação DataRecebimento desc, mesma de ConsultaService.Listar (T-23).
            Assert.Equal("V-1003", resultado.Linhas[0].VendaId);
            Assert.Equal("V-1002", resultado.Linhas[1].VendaId);
            Assert.Equal("V-1001", resultado.Linhas[2].VendaId);
        }

        [Fact]
        public void Obter_BancoVazio_TotalListadoZeroENenhumaLinha()
        {
            ResultadoRelatorioVendas resultado;
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                var consultaService = new ConsultaService(repositorio, repositorio);
                var dataSource = new RelatorioDataSource(consultaService);

                resultado = dataSource.Obter(new FiltroVendas());
            }

            Assert.Empty(resultado.Linhas);
            Assert.Equal(0.00m, resultado.TotalListado);
            Assert.Equal(0, resultado.QuantidadeSemValor);
        }

        [Fact]
        public void Obter_Com5005Vendas_RetornaTodasSemLimiteDe5000()
        {
            // Regra 15 (TASK.md Seção 1): "o relatório usa o mesmo filtro e sem o limite
            // de 5.000" — ao contrário de ConsultaService.Listar (T-23), que trunca em
            // 5.000 (ConsultaService.LimiteListagem).
            const int quantidadeTotal = 5005;
            var dataBase = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            using (var conn = AbrirConexao())
            {
                conn.Open();
                using (var transacao = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transacao;
                        cmd.CommandText =
                            "INSERT INTO FIN_VENDA (VENDA_ID, CLIENTE_ID, VALOR_TOTAL, STATUS, DATA_RECEBIMENTO, VERSAO) " +
                            "VALUES (@vendaId, @clienteId, @valorTotal, 0, @dataRecebimento, 0)";
                        cmd.Parameters.Add("@vendaId", FbDbType.VarChar, 50);
                        cmd.Parameters.Add("@clienteId", FbDbType.VarChar, 50);
                        cmd.Parameters.Add("@valorTotal", FbDbType.Decimal);
                        cmd.Parameters.Add("@dataRecebimento", FbDbType.TimeStamp);
                        cmd.Prepare();

                        for (int i = 0; i < quantidadeTotal; i++)
                        {
                            cmd.Parameters["@vendaId"].Value = "V-MASSA-" + i.ToString("D5");
                            cmd.Parameters["@clienteId"].Value = "CLI-MASSA";
                            cmd.Parameters["@valorTotal"].Value = 1.00m;
                            cmd.Parameters["@dataRecebimento"].Value = dataBase.AddMinutes(i);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transacao.Commit();
                }
            }

            ResultadoRelatorioVendas resultado;
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                var consultaService = new ConsultaService(repositorio, repositorio);
                var dataSource = new RelatorioDataSource(consultaService);

                resultado = dataSource.Obter(new FiltroVendas());
            }

            Assert.Equal(quantidadeTotal, resultado.Linhas.Count);
            Assert.Equal(5005.00m, resultado.TotalListado);
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
