using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace ERPFinanceiro.Tests.Infrastructure.Persistencia
{
    /// <summary>
    /// T-20 (Lote 5, TASK.md): harness xUnit de concorrência e idempotência contra
    /// Firebird real, app fechado (regra 11 — um único processo abre o `.fdb`; aqui o
    /// próprio processo de teste, com múltiplas <see cref="Task"/>/threads dentro dele,
    /// nunca um segundo processo). Três cenários:
    /// <list type="bullet">
    /// <item>Criação concorrente da mesma <c>vendaId</c> inexistente (2 quitações
    /// simultâneas via <c>Task.WhenAll</c>, cada uma com seu próprio
    /// <see cref="FinanceiroDbContext"/>/escopo de operação — RT-08/regra 5, réplica fiel
    /// da composição real de T-26). 20 execuções, conforme critério de aceite.</item>
    /// <item>Repetição sequencial (idempotência, P-2).</item>
    /// <item>Quitação e cancelamento concorrentes sobre a mesma venda Pendente.</item>
    /// </list>
    /// <para>
    /// <b>RL5-01 (Refatoração Lote-5, resolvida em T-26):</b> o risco de entidade
    /// "zumbi" originalmente documentado aqui e no <c>&lt;remarks&gt;</c> de
    /// <see cref="QuitacaoService"/> (T-18) — na corrida de <b>criação</b> (vendaId
    /// inexistente, upsert RN-06), se a 1ª tentativa de uma das duas quitações falhar
    /// por violação de <c>UNIQUE(VENDA_ID)</c>, a entidade <c>Added</c> "zumbi"
    /// continuava rastreada pelo mesmo <see cref="FinanceiroDbContext"/> — foi
    /// corrigido: <see cref="QuitacaoService"/>/<see cref="CancelamentoService"/> agora
    /// recebem um <see cref="IFabricaEscopoOperacao"/> e abrem um escopo/<c>DbContext</c>
    /// novo a cada tentativa de <see cref="ExecutorComRetry"/>, nunca reaproveitado. Os
    /// cenários abaixo (estatísticos, >= 20 corridas) continuam verdes; a prova
    /// determinística da correção está em
    /// <see cref="Executar_ColisaoDeUniqueForcadaNaPrimeiraTentativa_SegundaTentativaComEscopoNovoResolveIdempotente"/>,
    /// que força a colisão de <c>UNIQUE(VENDA_ID)</c> na 1ª tentativa (não apenas
    /// estatístico) e confirma que a 2ª tentativa resolve como sucesso idempotente.
    /// <see cref="ConcorrenciaException"/> continua sendo tratada como resultado
    /// esperado/conhecido nos cenários concorrentes abaixo — nem toda
    /// <see cref="ConcorrenciaException"/> observada é o risco de RL5-01 (conflito real
    /// de <c>VERSAO</c> entre duas operações legítimas também usa o mesmo tipo).
    /// </para>
    /// <para>
    /// Nota de ambiente (mesma de T-11/T-12/T-14/T-15/T-18/T-19): rodando de dentro do
    /// caminho sincronizado pelo OneDrive, `dotnet test`/`dotnet vstest` pode recusar
    /// carregar o adaptador xUnit net48. Contorno: copiar `bin/Debug/net48` já compilado
    /// para um diretório local fora do OneDrive e rodar `dotnet vstest
    /// ERPFinanceiro.Tests.dll` de lá — e então copiar
    /// `evidencia-execucao-T-20.txt` gerado ali de volta para este diretório do projeto.
    /// Pré-requisito RL4-02 (paralelização de assembly/coleção do xUnit desligada via
    /// `xunit.runner.json`) já resolvido antes desta tarefa começar.
    /// </para>
    /// </summary>
    public class ConcorrenciaIdempotenciaTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;
        private readonly StringBuilder _log = new StringBuilder();

        public ConcorrenciaIdempotenciaTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T20_Concorrencia_" + Guid.NewGuid().ToString("N") + ".fdb");
            _configuracao = new ConfiguracaoBancoTeste { CaminhoFdb = _caminhoFdb };

            new DbInitializer(_configuracao).Inicializar();

            _log.AppendLine("T-20 — harness de concorrência e idempotência (TASK.md Lote 5)");
            _log.AppendLine("Executado em: " + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            _log.AppendLine(new string('=', 78));
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
            public RelogioFixo(DateTime instanteUtc) { UtcNow = instanteUtc; }
            public DateTime UtcNow { get; set; }
        }

        private sealed class CorrelationContextFixo : ICorrelationContext
        {
            public CorrelationContextFixo(string correlationId) { CorrelationId = correlationId; }
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

        private static QuitarVendaCommand ComandoValido(string vendaId, string clienteId = "CLI-T20")
        {
            return new QuitarVendaCommand
            {
                VendaId = vendaId,
                ClienteId = clienteId,
                ValorTotal = 140.00m,
                Itens = new List<ItemVendaCommand>
                {
                    new ItemVendaCommand { ProdutoId = "PROD-T20-A", Quantidade = 2, PrecoUnitario = 50.00m },
                    new ItemVendaCommand { ProdutoId = "PROD-T20-B", Quantidade = 1, PrecoUnitario = 40.00m }
                }
            };
        }

        /// <summary>Resultado observado de uma tentativa concorrente (sucesso ou exceção conhecida).</summary>
        private sealed class ResultadoTentativa
        {
            public bool Sucesso { get; private set; }
            public string StatusResultante { get; private set; }
            public Exception ExcecaoConhecida { get; private set; }

            public static ResultadoTentativa DeSucesso(string status)
            {
                return new ResultadoTentativa { Sucesso = true, StatusResultante = status };
            }

            public static ResultadoTentativa DeExcecaoConhecida(Exception ex)
            {
                return new ResultadoTentativa { Sucesso = false, ExcecaoConhecida = ex };
            }

            public override string ToString()
            {
                return Sucesso
                    ? $"sucesso ({StatusResultante})"
                    : $"{ExcecaoConhecida.GetType().Name} (409, resultado esperado/conhecido)";
            }
        }

        /// <summary>
        /// Uma "operação" isolada de quitação: <see cref="QuitacaoService"/> recebe uma
        /// <see cref="FabricaEscopoOperacaoEf"/> (RL5-01, resolvida em T-26) — cada
        /// tentativa de <see cref="ExecutorComRetry"/> (T-16) abre seu próprio escopo/
        /// <see cref="FinanceiroDbContext"/>, nunca reaproveitado entre tentativas (réplica
        /// fiel da composição real de T-26/RT-08/regra 5). <see cref="ConcorrenciaException"/>
        /// é capturada aqui como resultado conhecido (409); qualquer outro tipo de exceção
        /// propaga para fora do helper e falha o teste.
        /// </summary>
        private ResultadoTentativa ExecutarQuitacaoIsolada(string vendaId, string correlationId, DateTime instanteUtc, string clienteId = "CLI-T20")
        {
            var servico = new QuitacaoService(
                new FabricaEscopoOperacaoEf(_configuracao),
                new RelogioFixo(instanteUtc),
                new CorrelationContextFixo(correlationId));

            try
            {
                var resultado = servico.Executar(ComandoValido(vendaId, clienteId));
                return ResultadoTentativa.DeSucesso(resultado.Status);
            }
            catch (ConcorrenciaException ex)
            {
                return ResultadoTentativa.DeExcecaoConhecida(ex);
            }
            catch (VendaJaCanceladaException ex)
            {
                // Cenário 3 (quitar/cancelar concorrentes): se o cancelamento ganhar a
                // corrida primeiro, a quitação concorrente encontra a venda já Cancelada
                // (T-08) — 409 legítimo, não um bug/risco do harness.
                return ResultadoTentativa.DeExcecaoConhecida(ex);
            }
        }

        private ResultadoTentativa ExecutarCancelamentoIsolado(string vendaId, string correlationId, DateTime instanteUtc, string motivo = null)
        {
            var servico = new CancelamentoService(
                new FabricaEscopoOperacaoEf(_configuracao),
                new RelogioFixo(instanteUtc));

            try
            {
                var resultado = servico.Cancelar(new CancelarVendaCommand
                {
                    VendaId = vendaId,
                    Motivo = motivo,
                    CorrelationId = correlationId
                });
                return ResultadoTentativa.DeSucesso(resultado.Status.ToString());
            }
            catch (VendaJaCanceladaException ex)
            {
                return ResultadoTentativa.DeExcecaoConhecida(ex);
            }
            catch (MotivoObrigatorioException ex)
            {
                return ResultadoTentativa.DeExcecaoConhecida(ex);
            }
            catch (ConcorrenciaException ex)
            {
                return ResultadoTentativa.DeExcecaoConhecida(ex);
            }
        }

        [Fact]
        public void Harness_ConcorrenciaEIdempotencia_T20()
        {
            var riscoDocumentadoReproduzido = ExecutarCenarioCriacaoConcorrente();
            ExecutarCenarioRepeticaoSequencial();
            ExecutarCenarioQuitarCancelarConcorrentes();

            _log.AppendLine(new string('=', 78));
            _log.AppendLine("Resultado final: 20/20 execuções do cenário de criação concorrente sem");
            _log.AppendLine("exceção não tratada (nenhuma exceção fora de ConcorrenciaException/");
            _log.AppendLine("VendaJaCanceladaException/MotivoObrigatorioException); em todas, exatamente");
            _log.AppendLine("1 venda e 1 histórico de quitação ao final.");
            _log.AppendLine(riscoDocumentadoReproduzido > 0
                ? $"Risco documentado em QuitacaoService.cs <remarks> REPRODUZIDO em {riscoDocumentadoReproduzido}/20 execuções (falso 409 na corrida de criação) — não corrigido aqui, ver T-26."
                : "Risco documentado em QuitacaoService.cs <remarks> NÃO reproduzido nesta execução (as 20 corridas de criação resolveram com sucesso duplo/idempotente).");

            SalvarEvidencia();
        }

        /// <summary>
        /// RL5-01 (Refatoração Lote-5) — critério de aceite: teste que força
        /// <b>deliberadamente</b> a colisão de <c>UNIQUE(VENDA_ID)</c> na 1ª tentativa
        /// (não apenas estatístico/N corridas) e confirma que a 2ª tentativa, com
        /// escopo/<c>DbContext</c> novo (RL5-01), resolve como sucesso idempotente (200),
        /// nunca <see cref="ConcorrenciaException"/> esgotada por causa da entidade
        /// "zumbi" documentada no <c>&lt;remarks&gt;</c> original de
        /// <see cref="QuitacaoService"/>.
        /// <para>
        /// Mecanismo: <see cref="FabricaComColisaoNaPrimeiraTentativa"/> decora
        /// <see cref="FabricaEscopoOperacaoEf"/> para, exatamente no momento em que a 1ª
        /// tentativa chamaria <c>IUnitOfWork.SalvarAlteracoes()</c> (ou seja, depois que
        /// <see cref="QuitacaoService"/> já leu "venda não existe" e decidiu criar —
        /// exatamente a janela de corrida do achado original), inserir e commitar — numa
        /// conexão/<see cref="FinanceiroDbContext"/> totalmente separada, simulando uma
        /// operação concorrente que venceu a corrida — uma venda já Quitada com o mesmo
        /// <c>VendaId</c>. O <c>SalvarAlteracoes()</c> real da 1ª tentativa então colide de
        /// verdade contra a constraint <c>UNIQUE(VENDA_ID)</c> do Firebird (T-05) e vira
        /// <see cref="ConcorrenciaException"/> (T-15), disparando o retry de
        /// <see cref="ExecutorComRetry"/> (T-16). A 2ª tentativa abre um escopo novo (a
        /// decoração só afeta a 1ª chamada a <c>Abrir()</c>) e relê a venda — já
        /// existente, Quitada — resolvendo pelo ramo idempotente de
        /// <see cref="QuitacaoService"/>, sem tentar inserir de novo.
        /// </para>
        /// </summary>
        [Fact]
        public void Executar_ColisaoDeUniqueForcadaNaPrimeiraTentativa_SegundaTentativaComEscopoNovoResolveIdempotente()
        {
            const string vendaId = "V-RL501-COLISAO-FORCADA";
            var instanteUtc = new DateTime(2026, 9, 22, 16, 0, 0, DateTimeKind.Utc);

            var fabricaComColisao = new FabricaComColisaoNaPrimeiraTentativa(
                new FabricaEscopoOperacaoEf(_configuracao),
                antesDoPrimeiroSalvar: () =>
                {
                    // Simula outra operação concorrente que venceu a corrida de criação e
                    // já comitou a venda ANTES do SalvarAlteracoes real da 1ª tentativa —
                    // numa conexão/contexto totalmente separados (nunca o mesmo DbContext
                    // da 1ª tentativa, que é justamente o ponto sendo testado).
                    using (var conn = AbrirConexao())
                    using (var ctx = new FinanceiroDbContext(conn))
                    {
                        var vendaConcorrente = Venda.CriarPorQuitacao(
                            vendaId, "CLI-RL501", 140.00m,
                            new[]
                            {
                                new VendaItem("PROD-RL501-A", 2, 50.00m),
                                new VendaItem("PROD-RL501-B", 1, 40.00m)
                            },
                            instanteUtc, instanteUtc, correlationId: "corr-rl501-concorrente");
                        new VendaRepository(ctx).Adicionar(vendaConcorrente);
                        new UnitOfWork(ctx).SalvarAlteracoes();
                    }
                });

            var servico = new QuitacaoService(
                fabricaComColisao,
                new RelogioFixo(instanteUtc),
                new CorrelationContextFixo("corr-rl501-tentativa"));

            // Antes da correção de RL5-01 (mesmo DbContext reaproveitado entre
            // tentativas), a entidade "Added" zumbi da 1ª tentativa continuaria
            // rastreada e a 2ª/3ª tentativa esgotariam com falso 409. Com a correção,
            // resolve 200 idempotente já na 2ª tentativa.
            var resultado = servico.Executar(ComandoValido(vendaId, clienteId: "CLI-RL501"));

            Assert.Equal("Quitada", resultado.Status);
            Assert.Equal(instanteUtc, resultado.DataQuitacaoUtc); // data da venda "concorrente", não uma nova.
            Assert.Equal(2, fabricaComColisao.ChamadasAbrir); // 1ª tentativa (colide) + 2ª tentativa (resolve).

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId(vendaId);

                Assert.NotNull(lida);
                Assert.Equal(StatusVenda.Quitada, lida.Status);
                // Só a venda "concorrente" existe — a 1ª tentativa da QuitacaoService NUNCA
                // conseguiu inserir a sua (colidiu antes de comitar); nenhuma duplicata.
                Assert.Equal(1, lida.Historico.Count(h => h.Operacao == Operacao.Quitacao));
                Assert.Single(lida.Historico, h => h.Operacao == Operacao.Quitacao && h.CorrelationId == "corr-rl501-concorrente");
                Assert.DoesNotContain(lida.Historico, h => h.CorrelationId == "corr-rl501-tentativa");
            }
        }

        /// <summary>
        /// Decora <see cref="IFabricaEscopoOperacao"/> para, só na 1ª chamada a
        /// <see cref="Abrir"/>, devolver um <see cref="IEscopoOperacao"/> cujo
        /// <see cref="IUnitOfWork.SalvarAlteracoes"/> executa
        /// <c>antesDoPrimeiroSalvar</c> antes de delegar ao real — usado para forçar
        /// deliberadamente a colisão de <c>UNIQUE(VENDA_ID)</c> no teste de RL5-01
        /// acima, no exato instante em que a operação de negócio tentaria comitar.
        /// </summary>
        private sealed class FabricaComColisaoNaPrimeiraTentativa : IFabricaEscopoOperacao
        {
            private readonly IFabricaEscopoOperacao _interna;
            private readonly Action _antesDoPrimeiroSalvar;

            public FabricaComColisaoNaPrimeiraTentativa(IFabricaEscopoOperacao interna, Action antesDoPrimeiroSalvar)
            {
                _interna = interna;
                _antesDoPrimeiroSalvar = antesDoPrimeiroSalvar;
            }

            public int ChamadasAbrir { get; private set; }

            public IEscopoOperacao Abrir()
            {
                ChamadasAbrir++;
                var escopoReal = _interna.Abrir();

                return ChamadasAbrir == 1
                    ? new EscopoComColisaoNaPrimeiraChamada(escopoReal, _antesDoPrimeiroSalvar)
                    : escopoReal;
            }

            private sealed class EscopoComColisaoNaPrimeiraChamada : IEscopoOperacao
            {
                private readonly IEscopoOperacao _interno;

                public EscopoComColisaoNaPrimeiraChamada(IEscopoOperacao interno, Action antesDoPrimeiroSalvar)
                {
                    _interno = interno;
                    UnitOfWork = new UnitOfWorkComColisaoNaPrimeiraChamada(interno.UnitOfWork, antesDoPrimeiroSalvar);
                }

                public IVendaRepository Repositorio => _interno.Repositorio;

                public IUnitOfWork UnitOfWork { get; }

                public void Dispose() => _interno.Dispose();
            }

            private sealed class UnitOfWorkComColisaoNaPrimeiraChamada : IUnitOfWork
            {
                private readonly IUnitOfWork _interno;
                private readonly Action _antesDoPrimeiroSalvar;
                private bool _jaExecutou;

                public UnitOfWorkComColisaoNaPrimeiraChamada(IUnitOfWork interno, Action antesDoPrimeiroSalvar)
                {
                    _interno = interno;
                    _antesDoPrimeiroSalvar = antesDoPrimeiroSalvar;
                }

                public void SalvarAlteracoes()
                {
                    if (!_jaExecutou)
                    {
                        _jaExecutou = true;
                        _antesDoPrimeiroSalvar();
                    }

                    _interno.SalvarAlteracoes();
                }
            }
        }

        /// <summary>
        /// Cenário 1 (critério de aceite literal): 20 execuções de 2 quitações concorrentes
        /// para a MESMA <c>vendaId</c> inexistente (corrida de criação/upsert, RN-06).
        /// </summary>
        private int ExecutarCenarioCriacaoConcorrente()
        {
            const int totalExecucoes = 20;
            var riscoReproduzido = 0;
            var ambosSucesso = 0;
            var umSucessoUmConflito = 0;

            _log.AppendLine();
            _log.AppendLine("Cenário 1 — criação concorrente da mesma vendaId (2 quitações via Task.WhenAll, 20 execuções)");
            _log.AppendLine(new string('-', 78));

            for (var i = 1; i <= totalExecucoes; i++)
            {
                var vendaId = $"V-T20-CRIACAO-{i:D2}";
                var instanteUtc = new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc).AddMinutes(i);

                ResultadoTentativa resultadoA = null;
                ResultadoTentativa resultadoB = null;

                var tarefaA = Task.Run(() => resultadoA = ExecutarQuitacaoIsolada(vendaId, $"corr-a-{i}", instanteUtc));
                var tarefaB = Task.Run(() => resultadoB = ExecutarQuitacaoIsolada(vendaId, $"corr-b-{i}", instanteUtc));

                // Qualquer exceção não capturada pelos helpers (isto é, diferente de
                // ConcorrenciaException) propaga aqui via AggregateException e FALHA o
                // teste — é exatamente o comportamento desejado pelo critério "nenhuma
                // exceção não tratada".
                Task.WaitAll(tarefaA, tarefaB);

                Assert.True(resultadoA.Sucesso || resultadoB.Sucesso,
                    $"Iteração {i}: as duas tentativas concorrentes falharam — não é o risco documentado (que previa no máximo 1 falha), indica bug real.");

                if (resultadoA.Sucesso && resultadoB.Sucesso)
                {
                    ambosSucesso++;
                }
                else
                {
                    umSucessoUmConflito++;
                    riscoReproduzido++;
                }

                using (var conn = AbrirConexao())
                using (var ctx = new FinanceiroDbContext(conn))
                {
                    var lida = new VendaRepository(ctx).ObterPorVendaId(vendaId);

                    Assert.NotNull(lida);
                    Assert.Equal(StatusVenda.Quitada, lida.Status);

                    var quitacoes = lida.Historico.Count(h => h.Operacao == Operacao.Quitacao);
                    Assert.Equal(1, quitacoes); // sempre 1 histórico de quitação, mesmo com retry/conflito

                    _log.AppendLine($"  [{i:D2}/20] vendaId={vendaId} | A={resultadoA} | B={resultadoB} | historico.Quitacao={quitacoes} | venda.Status={lida.Status}");
                }
            }

            _log.AppendLine();
            _log.AppendLine($"  Resumo: {ambosSucesso}/20 com ambas as tentativas em sucesso (idempotência resolvida sem conflito observável), {umSucessoUmConflito}/20 com 1 sucesso + 1 ConcorrenciaException (409 — risco documentado do <remarks> de QuitacaoService.cs, reproduzido ou não conforme acima).");

            return riscoReproduzido;
        }

        /// <summary>Cenário 2: repetição sequencial (não concorrente) — confirma idempotência (P-2).</summary>
        private void ExecutarCenarioRepeticaoSequencial()
        {
            _log.AppendLine();
            _log.AppendLine("Cenário 2 — repetição sequencial da mesma quitação (idempotência, P-2)");
            _log.AppendLine(new string('-', 78));

            var vendaId = "V-T20-SEQUENCIAL-001";
            var instante1 = new DateTime(2026, 9, 22, 13, 0, 0, DateTimeKind.Utc);
            var instante2 = instante1.AddHours(3);

            var primeira = ExecutarQuitacaoIsolada(vendaId, "corr-seq-1", instante1);
            var segunda = ExecutarQuitacaoIsolada(vendaId, "corr-seq-2", instante2);

            Assert.True(primeira.Sucesso, "1ª chamada sequencial deveria ter sucesso (sem concorrência).");
            Assert.True(segunda.Sucesso, "2ª chamada sequencial (repetição) deveria ter sucesso idempotente, não conflito.");

            using (var conn = AbrirConexao())
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var lida = new VendaRepository(ctx).ObterPorVendaId(vendaId);

                Assert.NotNull(lida);
                Assert.Equal(StatusVenda.Quitada, lida.Status);

                var quitacoes = lida.Historico.Count(h => h.Operacao == Operacao.Quitacao);
                Assert.Equal(1, quitacoes); // repetição não gera novo histórico (P-2)
                Assert.DoesNotContain(lida.Historico, h => h.CorrelationId == "corr-seq-2");

                _log.AppendLine($"  1ª chamada: {primeira} | 2ª chamada (repetição): {segunda} | historico.Quitacao={quitacoes}");
            }
        }

        /// <summary>Cenário 3: quitação e cancelamento concorrentes sobre a mesma venda Pendente.</summary>
        private void ExecutarCenarioQuitarCancelarConcorrentes()
        {
            const int totalExecucoes = 5;

            _log.AppendLine();
            _log.AppendLine("Cenário 3 — quitar/cancelar concorrentes na mesma venda Pendente");
            _log.AppendLine(new string('-', 78));

            for (var i = 1; i <= totalExecucoes; i++)
            {
                var vendaId = $"V-T20-QUITARCANCELAR-{i:D2}";
                var recebimentoUtc = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc).AddMinutes(i);
                var instanteOperacaoUtc = recebimentoUtc.AddHours(1);

                using (var conn = AbrirConexao())
                using (var ctx = new FinanceiroDbContext(conn))
                {
                    var venda = Venda.CriarPendente(
                        vendaId, "CLI-T20-QC", 140.00m,
                        new[]
                        {
                            new VendaItem("PROD-T20-A", 2, 50.00m),
                            new VendaItem("PROD-T20-B", 1, 40.00m)
                        },
                        recebimentoUtc);
                    new VendaRepository(ctx).Adicionar(venda);
                    new UnitOfWork(ctx).SalvarAlteracoes();
                }

                ResultadoTentativa resultadoQuitacao = null;
                ResultadoTentativa resultadoCancelamento = null;

                var tarefaQuitacao = Task.Run(() => resultadoQuitacao = ExecutarQuitacaoIsolada(vendaId, $"corr-qc-quit-{i}", instanteOperacaoUtc, clienteId: "CLI-T20-QC"));
                var tarefaCancelamento = Task.Run(() => resultadoCancelamento = ExecutarCancelamentoIsolado(vendaId, $"corr-qc-canc-{i}", instanteOperacaoUtc, motivo: null));

                Task.WaitAll(tarefaQuitacao, tarefaCancelamento);

                Assert.True(resultadoQuitacao.Sucesso || resultadoCancelamento.Sucesso,
                    $"Iteração {i}: quitação e cancelamento concorrentes falharam os dois — nenhum ganhou a corrida, indica bug real.");

                using (var conn = AbrirConexao())
                using (var ctx = new FinanceiroDbContext(conn))
                {
                    var lida = new VendaRepository(ctx).ObterPorVendaId(vendaId);

                    Assert.NotNull(lida);
                    Assert.True(lida.Status == StatusVenda.Quitada || lida.Status == StatusVenda.Cancelada,
                        $"Iteração {i}: estado final inesperado ({lida.Status}) — só Quitada ou Cancelada são válidos.");

                    // Exatamente 1 transição real além da Recebida original (quem ganhou a corrida).
                    var transicoesReais = lida.Historico.Count(h => h.Operacao == Operacao.Quitacao || h.Operacao == Operacao.Cancelamento);
                    Assert.Equal(1, transicoesReais);

                    _log.AppendLine($"  [{i}/{totalExecucoes}] vendaId={vendaId} | Quitação={resultadoQuitacao} | Cancelamento={resultadoCancelamento} | venda.Status={lida.Status} | transições reais={transicoesReais}");
                }
            }
        }

        private void SalvarEvidencia()
        {
            var caminho = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "evidencia-execucao-T-20.txt");
            File.WriteAllText(caminho, _log.ToString());
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
