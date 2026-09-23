using System;
using System.IO;
using System.Linq;
using ERPFinanceiro.Application.Commands;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Application.Servicos;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Domain.Exceptions;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ERPFinanceiro.Tests.Infrastructure.Persistencia
{
    /// <summary>
    /// Teste de integração real (T-19) de <see cref="CancelamentoService"/> contra Firebird
    /// embarcado real, mesmo mecanismo de schema/conexão de <see cref="VendaRepositoryTests"/>
    /// (T-14)/<see cref="UnitOfWorkTests"/> (T-15). Fica em Infrastructure.Persistencia (não
    /// em Application) porque exercita <see cref="VendaRepository"/>/<see cref="UnitOfWork"/>
    /// reais contra um `.fdb`, não dublês — <c>ERPFinanceiro.Application</c> não pode
    /// referenciar EF/Firebird (SDD 2.1), então o teste de integração de ponta a ponta mora
    /// no projeto que já tem essa referência.
    ///
    /// Roteiro cobrindo CA-02.1…02.6 (TASK.md T-19 / docs/contrato-v1.1.md Seção 3.2 / D-06 /
    /// D-08): Pendente cancela (motivo opcional); Quitada com motivo cancela; Quitada sem
    /// motivo falha sem alterar nada; Cancelada idempotente sem novo histórico; desconhecida
    /// cria Cancelada com 1 histórico e ValorTotal nulo (confirmado por consulta real ao
    /// `.fdb`, não só pelo objeto em memória).
    ///
    /// Nota de ambiente (mesma de T-11/T-12/T-14/T-15): rodando de dentro do caminho
    /// sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar carregar o
    /// adaptador xUnit net48 (FileLoadException, HRESULT 0x80131515). Contorno: copiar
    /// bin/Debug/net48 já compilado para um diretório local fora do OneDrive e rodar
    /// `dotnet vstest ERPFinanceiro.Tests.dll` de lá. Também sujeito ao risco conhecido
    /// (RL4-02, ainda não corrigido) de `AccessViolationException` intermitente do Firebird
    /// Embedded sob execução paralela de testes — reexecutar antes de assumir bug real.
    /// </summary>
    public class CancelamentoServiceTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;
        private readonly RelogioFixo _relogio = new RelogioFixo(new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc));

        public CancelamentoServiceTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T19_CancelamentoServiceTests_" + Guid.NewGuid().ToString("N") + ".fdb");
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

        private sealed class RelogioFixo : IClock
        {
            public RelogioFixo(DateTime utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTime UtcNow { get; set; }
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

        private CancelamentoService CriarServico()
        {
            return new CancelamentoService(new FabricaEscopoOperacaoEf(_configuracao), _relogio);
        }

        [Fact]
        public void Cancelar_VendaPendente_Cancela()
        {
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarPendente(
                    vendaId: "V-T19-PENDENTE",
                    clienteId: "CLI-T19",
                    valorTotal: 100.00m,
                    itens: new[] { new VendaItem("PROD-T19", 1, 100.0000m) },
                    dataRecebimentoUtc: _relogio.UtcNow.AddHours(-1));

                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }

            {
                var servico = CriarServico();

                // CA-02.1: Pendente cancela sem motivo (opcional nesse caso, T-09).
                var resultado = servico.Cancelar(new CancelarVendaCommand
                {
                    VendaId = "V-T19-PENDENTE",
                    CorrelationId = "corr-t19-pendente"
                });

                Assert.Equal(StatusVenda.Cancelada, resultado.Status);
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId("V-T19-PENDENTE");

                Assert.Equal(StatusVenda.Cancelada, lida.Status);
                Assert.Equal(2, lida.Historico.Count); // Recebida + Cancelamento
                var cancelamento = lida.Historico.Single(h => h.Operacao == Operacao.Cancelamento);
                Assert.Equal("corr-t19-pendente", cancelamento.CorrelationId);
            }
        }

        [Fact]
        public void Cancelar_VendaQuitadaComMotivo_Cancela()
        {
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarPorQuitacao(
                    vendaId: "V-T19-QUITADA-OK",
                    clienteId: "CLI-T19",
                    valorTotal: 250.00m,
                    itens: new[] { new VendaItem("PROD-T19-Q", 1, 250.0000m) },
                    dataRecebimentoUtc: _relogio.UtcNow.AddHours(-2),
                    dataQuitacaoUtc: _relogio.UtcNow.AddHours(-1));

                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }

            {
                var servico = CriarServico();

                // CA-02.2: Quitada + motivo cancela (D-06/S-05).
                var resultado = servico.Cancelar(new CancelarVendaCommand
                {
                    VendaId = "V-T19-QUITADA-OK",
                    Motivo = "Cliente desistiu da compra",
                    CorrelationId = "corr-t19-quitada-ok"
                });

                Assert.Equal(StatusVenda.Cancelada, resultado.Status);
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId("V-T19-QUITADA-OK");

                Assert.Equal(StatusVenda.Cancelada, lida.Status);
                Assert.Equal("Cliente desistiu da compra", lida.MotivoCancelamento);
                Assert.Equal(3, lida.Historico.Count); // Recebida + Quitacao + Cancelamento
            }
        }

        [Fact]
        public void Cancelar_VendaQuitadaSemMotivo_LancaSemAlterarNada()
        {
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarPorQuitacao(
                    vendaId: "V-T19-QUITADA-SEM-MOTIVO",
                    clienteId: "CLI-T19",
                    valorTotal: 300.00m,
                    itens: new[] { new VendaItem("PROD-T19-SM", 1, 300.0000m) },
                    dataRecebimentoUtc: _relogio.UtcNow.AddHours(-2),
                    dataQuitacaoUtc: _relogio.UtcNow.AddHours(-1));

                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }

            {
                var servico = CriarServico();

                // CA-02.3: Quitada sem motivo lança MotivoObrigatorioException (T-09),
                // sem alterar estado — mapeada a 409 MOTIVO_OBRIGATORIO pela Api (T-28).
                Assert.Throws<MotivoObrigatorioException>(() => servico.Cancelar(new CancelarVendaCommand
                {
                    VendaId = "V-T19-QUITADA-SEM-MOTIVO"
                }));
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId("V-T19-QUITADA-SEM-MOTIVO");

                Assert.Equal(StatusVenda.Quitada, lida.Status); // nada mudou
                Assert.Null(lida.MotivoCancelamento);
                Assert.Equal(2, lida.Historico.Count); // Recebida + Quitacao, sem 3º registro
            }
        }

        [Fact]
        public void Cancelar_VendaJaCancelada_IdempotenteSemNovoHistorico()
        {
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarPendente(
                    vendaId: "V-T19-JA-CANCELADA",
                    clienteId: "CLI-T19",
                    valorTotal: 50.00m,
                    itens: new[] { new VendaItem("PROD-T19-JC", 1, 50.0000m) },
                    dataRecebimentoUtc: _relogio.UtcNow.AddHours(-1));

                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }

            // Primeiro cancelamento: efetiva.
            CriarServico().Cancelar(new CancelarVendaCommand { VendaId = "V-T19-JA-CANCELADA" });

            // CA-02.4: repetição sobre venda já Cancelada é idempotente.
            var segundoResultado = CriarServico().Cancelar(new CancelarVendaCommand { VendaId = "V-T19-JA-CANCELADA" });

            Assert.Equal(StatusVenda.Cancelada, segundoResultado.Status);

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId("V-T19-JA-CANCELADA");

                Assert.Equal(StatusVenda.Cancelada, lida.Status);
                Assert.Equal(2, lida.Historico.Count); // Recebida + Cancelamento — sem 2º Cancelamento
                Assert.Single(lida.Historico, h => h.Operacao == Operacao.Cancelamento);
            }
        }

        [Fact]
        public void Cancelar_VendaDesconhecida_CriaCanceladaComUmHistoricoEValorTotalNulo()
        {
            {
                var servico = CriarServico();

                // CA-02.5/02.6: venda desconhecida -> cria já Cancelada (T-10, D-08/ADR-007).
                var resultado = servico.Cancelar(new CancelarVendaCommand
                {
                    VendaId = "V-T19-DESCONHECIDA",
                    Motivo = "Cancelamento de venda nunca recebida",
                    CorrelationId = "corr-t19-desconhecida"
                });

                Assert.Equal(StatusVenda.Cancelada, resultado.Status);
            }

            // Confirmação por consulta real ao .fdb (não só o objeto em memória desta
            // chamada), conforme exigido pela linha T-19 do TASK.md.
            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId("V-T19-DESCONHECIDA");

                Assert.NotNull(lida);
                Assert.Equal(StatusVenda.Cancelada, lida.Status);
                Assert.Null(lida.ClienteId);
                Assert.Null(lida.ValorTotal);
                Assert.Empty(lida.Itens);
                Assert.Single(lida.Historico);

                var historico = lida.Historico.Single();
                Assert.Equal(Operacao.Cancelamento, historico.Operacao);
                Assert.Null(historico.StatusAnterior);
                Assert.Equal(StatusVenda.Cancelada, historico.StatusNovo);
                Assert.Equal("Cancelamento de venda nunca recebida", historico.Motivo);
                Assert.Equal("corr-t19-desconhecida", historico.CorrelationId);
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
