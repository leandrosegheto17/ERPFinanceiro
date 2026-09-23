using System;
using System.IO;
using System.Linq;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ERPFinanceiro.Tests.Infrastructure.Persistencia
{
    /// <summary>
    /// Teste de integração real (T-23) de <see cref="ConsultaService.Listar"/> contra
    /// Firebird embarcado real, mesmo mecanismo de schema/conexão já usado por
    /// <see cref="VendaRepositoryTests"/> (T-14). Cobre os 2 critérios de aceite de
    /// T-23: (1) com massa equivalente ao seed de T-06 (3 vendas), retorna todas em
    /// ordem correta (DataRecebimento desc), com nulos preservados no DTO; (2) com
    /// 5.001 linhas, retorna 5.000 e Truncado=true.
    ///
    /// Fica em Infrastructure.Persistencia (não em Application) porque exercita
    /// <see cref="VendaRepository"/> real — a cobertura puramente de
    /// Application/lógica de mapeamento de <c>ConsultaService</c> (ObterDetalhe/
    /// ObterStatus) já está em <c>ERPFinanceiro.Tests.Application.ConsultaServiceTests</c>
    /// (T-24), com repositório mockado.
    ///
    /// Nota de ambiente (mesma de T-11/T-12/T-14): rodando de dentro do caminho
    /// sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar carregar o
    /// adaptador xUnit net48 (FileLoadException, HRESULT 0x80131515). Contorno: copiar
    /// bin/Debug/net48 já compilado para um diretório local fora do OneDrive e rodar
    /// `dotnet vstest ERPFinanceiro.Tests.dll` de lá.
    /// </summary>
    public class ConsultaServiceListarTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;

        public ConsultaServiceListarTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T23_ConsultaServiceListarTests_" + Guid.NewGuid().ToString("N") + ".fdb");
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
        public void Listar_ComMassaEquivalenteAoSeedT06_RetornaTodasOrdenadasPorDataRecebimentoDescComNulosPreservados()
        {
            // Mesma composição de database/02-seed.sql (T-06): Quitada, Pendente,
            // Cancelada-desconhecida (ClienteId/ValorTotal NULL, ADR-007/D-08).
            var dataV1001 = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
            var dataV1002 = new DateTime(2026, 9, 5, 9, 15, 0, DateTimeKind.Utc);
            var dataV1003 = new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc);

            var v1001 = Venda.CriarPorQuitacao(
                vendaId: "V-1001",
                clienteId: "CLI-001",
                valorTotal: 500.50m,
                itens: new[] { new VendaItem("PROD-001", 2, 150.0000m) },
                dataRecebimentoUtc: dataV1001,
                dataQuitacaoUtc: new DateTime(2026, 9, 3, 14, 30, 0, DateTimeKind.Utc));

            var v1002 = Venda.CriarPendente(
                vendaId: "V-1002",
                clienteId: "CLI-002",
                valorTotal: 450.50m,
                itens: new[] { new VendaItem("PROD-003", 3, 100.0000m) },
                dataRecebimentoUtc: dataV1002);

            var v1003 = Venda.CriarCancelada(
                vendaId: "V-1003",
                motivo: "Venda desconhecida cancelada pelo cliente",
                dataCancelamentoUtc: dataV1003);

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                repositorio.Adicionar(v1001);
                repositorio.Adicionar(v1002);
                repositorio.Adicionar(v1003);
                ctx.SaveChanges();
            }

            ResultadoListagemVendas resultado;
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                var servico = new ConsultaService(repositorio, repositorio);

                resultado = servico.Listar(new FiltroVendas());
            }

            Assert.False(resultado.Truncado);
            Assert.Equal(3, resultado.Itens.Count);

            // Ordenação DataRecebimento desc: V-1003 (07/09) > V-1002 (05/09) > V-1001 (01/09).
            Assert.Equal("V-1003", resultado.Itens[0].VendaId);
            Assert.Equal("V-1002", resultado.Itens[1].VendaId);
            Assert.Equal("V-1001", resultado.Itens[2].VendaId);

            var dtoV1003 = resultado.Itens[0];
            Assert.Equal(StatusVenda.Cancelada, dtoV1003.Status);
            Assert.Null(dtoV1003.ClienteId);
            Assert.Null(dtoV1003.ValorTotal);

            var dtoV1002 = resultado.Itens[1];
            Assert.Equal(StatusVenda.Pendente, dtoV1002.Status);
            Assert.Equal("CLI-002", dtoV1002.ClienteId);
            Assert.Equal(450.50m, dtoV1002.ValorTotal);
            Assert.Null(dtoV1002.DataQuitacao);
            Assert.Null(dtoV1002.DataCancelamento);

            var dtoV1001 = resultado.Itens[2];
            Assert.Equal(StatusVenda.Quitada, dtoV1001.Status);
            Assert.Equal("CLI-001", dtoV1001.ClienteId);
            Assert.Equal(500.50m, dtoV1001.ValorTotal);
            Assert.NotNull(dtoV1001.DataQuitacao);
        }

        [Fact]
        public void Listar_Com5001Vendas_Retorna5000ETruncadoTrue()
        {
            const int quantidadeTotal = 5001;
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
                            cmd.Parameters["@valorTotal"].Value = 10.00m;
                            cmd.Parameters["@dataRecebimento"].Value = dataBase.AddMinutes(i);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transacao.Commit();
                }
            }

            ResultadoListagemVendas resultado;
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repositorio = new VendaRepository(ctx);
                var servico = new ConsultaService(repositorio, repositorio);

                resultado = servico.Listar(new FiltroVendas());
            }

            Assert.True(resultado.Truncado);
            Assert.Equal(ConsultaService.LimiteListagem, resultado.Itens.Count);

            // A mais recente (maior DATA_RECEBIMENTO, índice quantidadeTotal - 1) deve vir primeiro.
            Assert.Equal("V-MASSA-05000", resultado.Itens.First().VendaId);
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
