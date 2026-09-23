using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ERPFinanceiro.Application.Commands;
using ERPFinanceiro.Application.Exceptions;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Application.Servicos;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Domain.Exceptions;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ERPFinanceiro.Tests.Application
{
    /// <summary>
    /// Teste de integração real (T-18) de <see cref="QuitacaoService"/> contra Firebird
    /// embarcado real, mesmo mecanismo de schema/conexão já usado por
    /// <see cref="ERPFinanceiro.Tests.Infrastructure.Persistencia.VendaRepositoryTests"/> (T-14)/
    /// <see cref="ERPFinanceiro.Tests.Infrastructure.Persistencia.UnitOfWorkTests"/> (T-15).
    ///
    /// Cobre os cenários descritos na linha T-18 do TASK.md: venda inexistente cria+quita
    /// (upsert RN-06); venda Pendente quita; venda já Quitada devolve o resultado
    /// original sem novo histórico (P-2); payload inválido/valorTotal divergente lançam
    /// <see cref="ValidacaoException"/> (T-17); venda Pendente com payload divergente
    /// lança <see cref="DadosDivergentesException"/> (D-03/I-04); venda Cancelada lança
    /// <see cref="VendaJaCanceladaException"/> (T-08, mapeada a 409 pela Api em T-28).
    ///
    /// Nota de ambiente (mesma de T-11/T-12/T-14/T-15): rodando de dentro do caminho
    /// sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar carregar o
    /// adaptador xUnit net48. Contorno: copiar bin/Debug/net48 já compilado para um
    /// diretório local fora do OneDrive e rodar `dotnet vstest ERPFinanceiro.Tests.dll`
    /// de lá. Também: risco conhecido (RL4-02) de `AccessViolationException`
    /// intermitente do Firebird Embedded sob paralelização de testes — não é regressão
    /// desta tarefa; reexecutar isoladamente se ocorrer.
    /// </summary>
    public class QuitacaoServiceTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;

        public QuitacaoServiceTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T18_QuitacaoServiceTests_" + Guid.NewGuid().ToString("N") + ".fdb");
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
            public RelogioFixo(DateTime instanteUtc)
            {
                UtcNow = instanteUtc;
            }

            public DateTime UtcNow { get; set; }
        }

        private sealed class CorrelationContextFixo : ICorrelationContext
        {
            public CorrelationContextFixo(string correlationId)
            {
                CorrelationId = correlationId;
            }

            public string CorrelationId { get; }
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

        private static QuitarVendaCommand ComandoValido(string vendaId, string clienteId = "CLI-T18")
        {
            return new QuitarVendaCommand
            {
                VendaId = vendaId,
                ClienteId = clienteId,
                ValorTotal = 140.00m,
                Itens = new List<ItemVendaCommand>
                {
                    new ItemVendaCommand { ProdutoId = "PROD-T18-A", Quantidade = 2, PrecoUnitario = 50.00m },
                    new ItemVendaCommand { ProdutoId = "PROD-T18-B", Quantidade = 1, PrecoUnitario = 40.00m }
                }
            };
        }

        [Fact]
        public void Executar_VendaInexistente_CriaEQuitaComUmaLinhaDeHistoricoPorTransicaoReal()
        {
            var instanteUtc = new DateTime(2026, 9, 22, 14, 5, 0, DateTimeKind.Utc);

            {
                var servico = new QuitacaoService(new FabricaEscopoOperacaoEf(_configuracao), new RelogioFixo(instanteUtc), new CorrelationContextFixo("corr-t18-001"));

                var resultado = servico.Executar(ComandoValido("V-T18-001"));

                Assert.Equal("Quitada", resultado.Status);
                Assert.Equal(instanteUtc, resultado.DataQuitacaoUtc);
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId("V-T18-001");

                Assert.NotNull(lida);
                Assert.Equal(StatusVenda.Quitada, lida.Status);
                Assert.Equal(2, lida.Itens.Count);
                // CriarPorQuitacao (T-07) grava as 2 transições reais da vida da venda
                // (Recebida->Pendente implícita, Pendente->Quitada), não 1 — a venda
                // nasce diretamente no estado final, mas a auditoria completa exige as
                // duas entradas (ver Venda.CriarPorQuitacao, T-07).
                Assert.Equal(2, lida.Historico.Count);
                Assert.Single(lida.Historico, h => h.Operacao == Operacao.Recebida);
                Assert.Single(lida.Historico, h => h.Operacao == Operacao.Quitacao && h.CorrelationId == "corr-t18-001");
            }
        }

        [Fact]
        public void Executar_VendaPendente_QuitaEAdicionaUmaLinhaDeHistorico()
        {
            var recebimentoUtc = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var quitacaoUtc = new DateTime(2026, 9, 22, 15, 0, 0, DateTimeKind.Utc);

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarPendente(
                    "V-T18-002", "CLI-T18", 140.00m,
                    new[]
                    {
                        new VendaItem("PROD-T18-A", 2, 50.00m),
                        new VendaItem("PROD-T18-B", 1, 40.00m)
                    },
                    recebimentoUtc);
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }

            {
                var servico = new QuitacaoService(new FabricaEscopoOperacaoEf(_configuracao), new RelogioFixo(quitacaoUtc), new CorrelationContextFixo("corr-t18-002"));

                var resultado = servico.Executar(ComandoValido("V-T18-002"));

                Assert.Equal("Quitada", resultado.Status);
                Assert.Equal(quitacaoUtc, resultado.DataQuitacaoUtc);
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId("V-T18-002");

                Assert.Equal(StatusVenda.Quitada, lida.Status);
                Assert.Equal(2, lida.Historico.Count); // Recebida (na criação) + Quitacao (1 transição real).
                Assert.Single(lida.Historico, h => h.Operacao == Operacao.Quitacao && h.CorrelationId == "corr-t18-002");
            }
        }

        [Fact]
        public void Executar_VendaJaQuitada_DevolveResultadoOriginalSemNovoHistorico()
        {
            var recebimentoUtc = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var quitacaoOriginalUtc = new DateTime(2026, 9, 22, 11, 0, 0, DateTimeKind.Utc);
            var tentativaRepetidaUtc = new DateTime(2026, 9, 22, 20, 0, 0, DateTimeKind.Utc);

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarPorQuitacao(
                    "V-T18-003", "CLI-T18", 140.00m,
                    new[]
                    {
                        new VendaItem("PROD-T18-A", 2, 50.00m),
                        new VendaItem("PROD-T18-B", 1, 40.00m)
                    },
                    recebimentoUtc, quitacaoOriginalUtc, correlationId: "corr-t18-003-original");
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }

            {
                var servico = new QuitacaoService(new FabricaEscopoOperacaoEf(_configuracao), new RelogioFixo(tentativaRepetidaUtc), new CorrelationContextFixo("corr-t18-003-repeticao"));

                var resultado = servico.Executar(ComandoValido("V-T18-003"));

                // Idempotência (P-2): devolve a dataQuitacao ORIGINAL, não a da repetição.
                Assert.Equal("Quitada", resultado.Status);
                Assert.Equal(quitacaoOriginalUtc, resultado.DataQuitacaoUtc);
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId("V-T18-003");

                Assert.Equal(StatusVenda.Quitada, lida.Status);
                // Sem novo histórico: continua com só as 2 entradas da criação original.
                Assert.Equal(2, lida.Historico.Count);
                Assert.DoesNotContain(lida.Historico, h => h.CorrelationId == "corr-t18-003-repeticao");
            }
        }

        [Fact]
        public void Executar_VendaCancelada_LancaVendaJaCanceladaExceptionSemAlterarDados()
        {
            var cancelamentoUtc = new DateTime(2026, 9, 22, 9, 0, 0, DateTimeKind.Utc);

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarCancelada("V-T18-004", "Cliente desistiu", cancelamentoUtc);
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }

            {
                var servico = new QuitacaoService(new FabricaEscopoOperacaoEf(_configuracao), new RelogioFixo(cancelamentoUtc.AddHours(1)), new CorrelationContextFixo("corr-t18-004"));

                var comando = new QuitarVendaCommand
                {
                    VendaId = "V-T18-004",
                    ClienteId = "CLI-QUALQUER",
                    ValorTotal = 10.00m,
                    Itens = new List<ItemVendaCommand> { new ItemVendaCommand { ProdutoId = "P-X", Quantidade = 1, PrecoUnitario = 10.00m } }
                };

                Assert.Throws<VendaJaCanceladaException>(() => servico.Executar(comando));
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId("V-T18-004");

                Assert.Equal(StatusVenda.Cancelada, lida.Status);
                Assert.Single(lida.Historico); // sem alteração: continua só o Cancelamento original.
            }
        }

        [Fact]
        public void Executar_PayloadInvalido_ItensVazio_LancaValidacaoExceptionSemPersistir()
        {
            {
                var servico = new QuitacaoService(new FabricaEscopoOperacaoEf(_configuracao), new RelogioFixo(DateTime.UtcNow), new CorrelationContextFixo("corr-t18-005"));

                var comando = new QuitarVendaCommand
                {
                    VendaId = "V-T18-005",
                    ClienteId = "CLI-T18",
                    ValorTotal = 10.00m,
                    Itens = new List<ItemVendaCommand>()
                };

                var excecao = Assert.Throws<ValidacaoException>(() => servico.Executar(comando));
                Assert.Equal("PAYLOAD_INVALIDO", excecao.Erro.Codigo);
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                Assert.Null(new VendaRepository(ctx).ObterPorVendaId("V-T18-005"));
            }
        }

        [Fact]
        public void Executar_ValorTotalDivergenteDaSomaDosItens_LancaValidacaoExceptionSemPersistir()
        {
            {
                var servico = new QuitacaoService(new FabricaEscopoOperacaoEf(_configuracao), new RelogioFixo(DateTime.UtcNow), new CorrelationContextFixo("corr-t18-006"));

                var comando = new QuitarVendaCommand
                {
                    VendaId = "V-T18-006",
                    ClienteId = "CLI-T18",
                    ValorTotal = 1250.50m, // soma real dos itens = 1240.00 (fora da tolerância de 0,01)
                    Itens = new List<ItemVendaCommand>
                    {
                        new ItemVendaCommand { ProdutoId = "P-01", Quantidade = 2, PrecoUnitario = 500.00m },
                        new ItemVendaCommand { ProdutoId = "P-02", Quantidade = 1, PrecoUnitario = 240.00m }
                    }
                };

                var excecao = Assert.Throws<ValidacaoException>(() => servico.Executar(comando));
                Assert.Equal("VALOR_TOTAL_DIVERGENTE", excecao.Erro.Codigo);
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                Assert.Null(new VendaRepository(ctx).ObterPorVendaId("V-T18-006"));
            }
        }

        [Fact]
        public void Executar_VendaPendenteComPayloadDivergente_LancaDadosDivergentesExceptionSemAlterarDados()
        {
            var recebimentoUtc = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var venda = Venda.CriarPendente(
                    "V-T18-007", "CLI-T18-ORIGINAL", 140.00m,
                    new[]
                    {
                        new VendaItem("PROD-T18-A", 2, 50.00m),
                        new VendaItem("PROD-T18-B", 1, 40.00m)
                    },
                    recebimentoUtc);
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }

            {
                var servico = new QuitacaoService(new FabricaEscopoOperacaoEf(_configuracao), new RelogioFixo(recebimentoUtc.AddHours(1)), new CorrelationContextFixo("corr-t18-007"));

                // Mesmo vendaId, mas clienteId diferente do registrado -> DADOS_DIVERGENTES.
                var comandoDivergente = ComandoValido("V-T18-007", clienteId: "CLI-T18-DIFERENTE");

                var excecao = Assert.Throws<DadosDivergentesException>(() => servico.Executar(comandoDivergente));
                Assert.Equal("V-T18-007", excecao.VendaId);
            }

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId("V-T18-007");

                Assert.Equal(StatusVenda.Pendente, lida.Status); // sem alteração
                Assert.Single(lida.Historico);
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
