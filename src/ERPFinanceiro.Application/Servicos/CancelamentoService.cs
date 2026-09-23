using System;
using ERPFinanceiro.Application.Commands;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;

namespace ERPFinanceiro.Application.Servicos
{
    /// <summary>
    /// Serviço de aplicação de cancelamento de venda (T-19, O-06), insumo de
    /// <c>POST /api/vendas/cancelamento</c> (T-32). Cobre os quatro cenários do
    /// <c>docs/contrato-v1.1.md</c> Seção 3.2 / D-06 / D-08:
    /// <list type="bullet">
    /// <item>Pendente: cancela via <see cref="Venda.Cancelar"/>, motivo opcional (T-09).</item>
    /// <item>Quitada: cancela via <see cref="Venda.Cancelar"/> exigindo motivo — se ausente,
    /// <see cref="ERPFinanceiro.Domain.Exceptions.MotivoObrigatorioException"/> propaga sem
    /// alterar nada (o próprio agregado garante isso, T-09); mapeada a 409 pela Api (T-28).</item>
    /// <item>Já Cancelada: idempotente — <see cref="Venda.Cancelar"/> devolve
    /// <c>false</c> ("sem mudança"), sem novo histórico (T-08/T-09).</item>
    /// <item>Desconhecida (não encontrada): cria via <see cref="Venda.CriarCancelada"/>
    /// (T-10, D-08/ADR-007).</item>
    /// </list>
    /// Toda a operação (reler, reavaliar, salvar) roda dentro de
    /// <see cref="Servicos.ExecutorComRetry"/> (T-16): se <see cref="IUnitOfWork.SalvarAlteracoes"/>
    /// lançar <see cref="Exceptions.ConcorrenciaException"/> (conflito de <c>VERSAO</c> ou
    /// violação de <c>UNIQUE(VENDA_ID)</c>, T-15), a operação inteira — inclusive a releitura
    /// da venda — é repetida, no máximo <see cref="Servicos.ExecutorComRetry.MaximoTentativas"/>
    /// vezes, antes de propagar (409 <c>CONFLITO_CONCORRENCIA</c>, Api).
    /// </summary>
    /// <remarks>
    /// <b>RL5-01 (Refatoração Lote-5, resolvida em T-26):</b> mesmo mecanismo de
    /// <see cref="QuitacaoService"/> — cada tentativa de <see cref="Servicos.ExecutorComRetry"/>
    /// abre seu próprio <see cref="IEscopoOperacao"/> (<see cref="IFabricaEscopoOperacao.Abrir"/>),
    /// nunca reaproveitando o <c>DbContext</c> de uma tentativa anterior. Resolve o
    /// mesmo risco de entidade "zumbi" no ramo "desconhecida" (<see cref="Venda.CriarCancelada"/>,
    /// upsert de cancelamento) sob corrida de criação concorrente.
    /// </remarks>
    public class CancelamentoService
    {
        private readonly IFabricaEscopoOperacao _fabricaEscopo;
        private readonly IClock _clock;
        private readonly IAppLogger _logger;

        /// <param name="fabricaEscopo">
        /// Fábrica de <see cref="IEscopoOperacao"/> (RL5-01): um novo escopo/<c>DbContext</c>
        /// é aberto a cada tentativa de <see cref="Servicos.ExecutorComRetry"/> dentro de
        /// <see cref="Cancelar"/>, nunca compartilhado entre tentativas.
        /// </param>
        /// <param name="logger">
        /// Opcional (nulo permitido): grava uma linha correlacionável descrevendo o
        /// resultado do cancelamento (regra 7 do TASK.md Seção 1), mas a ausência de
        /// logger nunca deve impedir a operação de negócio em si.
        /// </param>
        public CancelamentoService(IFabricaEscopoOperacao fabricaEscopo, IClock clock, IAppLogger logger = null)
        {
            _fabricaEscopo = fabricaEscopo ?? throw new ArgumentNullException(nameof(fabricaEscopo));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _logger = logger;
        }

        /// <summary>
        /// Cancela a venda identificada por <see cref="CancelarVendaCommand.VendaId"/>,
        /// conforme os quatro cenários documentados na classe. Validação de payload
        /// (ex.: <c>vendaId</c> vazio) fica a cargo da Api/controller (T-32) — aqui só a
        /// pré-condição mínima de o comando/VendaId existirem, via
        /// <see cref="ArgumentNullException"/>/<see cref="ArgumentException"/> (erro de
        /// programação do chamador, não um cenário de negócio do contrato).
        /// </summary>
        public ResultadoCancelamento Cancelar(CancelarVendaCommand comando)
        {
            if (comando == null)
            {
                throw new ArgumentNullException(nameof(comando));
            }

            if (string.IsNullOrWhiteSpace(comando.VendaId))
            {
                throw new ArgumentException("VendaId é obrigatório.", nameof(comando));
            }

            // RL5-01: um IEscopoOperacao (e portanto um DbContext) novo por tentativa —
            // aberto e descartado dentro do próprio callback, nunca reaproveitado entre
            // as até 3 tentativas de ExecutorComRetry (T-16).
            return ExecutorComRetry.Executar(() =>
            {
                using (var escopo = _fabricaEscopo.Abrir())
                {
                    return CancelarInterno(escopo.Repositorio, escopo.UnitOfWork, comando);
                }
            });
        }

        /// <summary>
        /// Um "ciclo" completo de reler + reavaliar + salvar — chamado de novo a cada
        /// tentativa de <see cref="ExecutorComRetry"/>, sempre relendo a venda do zero
        /// (nunca reaproveitando o agregado de uma tentativa anterior), para que um
        /// conflito de concorrência resolvido por outra operação nesse meio-tempo seja
        /// enxergado corretamente (ex.: a venda desconhecida que esta chamada tentava
        /// criar pode já ter sido criada por outra requisição concorrente — a releitura
        /// vai encontrá-la e cair no ramo idempotente/normal em vez de tentar duplicar).
        /// </summary>
        private ResultadoCancelamento CancelarInterno(IVendaRepository repositorio, IUnitOfWork unitOfWork, CancelarVendaCommand comando)
        {
            var dataCancelamentoUtc = _clock.UtcNow;
            var venda = repositorio.ObterPorVendaId(comando.VendaId);

            if (venda == null)
            {
                // Desconhecida: cria já Cancelada (T-10, D-08/ADR-007) — sem cliente,
                // valor ou itens.
                venda = Venda.CriarCancelada(comando.VendaId, comando.Motivo, dataCancelamentoUtc, comando.CorrelationId);
                repositorio.Adicionar(venda);
            }
            else
            {
                // Pendente (motivo opcional)/Quitada (motivo obrigatório, senão
                // MotivoObrigatorioException sem alterar nada, T-09)/Cancelada
                // (idempotente, sem novo histórico, T-08/T-09) — toda a regra já vive
                // no agregado.
                venda.Cancelar(dataCancelamentoUtc, comando.Motivo, comando.CorrelationId);
            }

            unitOfWork.SalvarAlteracoes();

            _logger?.Registrar(
                $"Cancelamento processado para venda '{venda.VendaId}' (status resultante: {venda.Status}).",
                comando.CorrelationId);

            return new ResultadoCancelamento
            {
                VendaId = venda.VendaId,
                Status = venda.Status
            };
        }
    }
}
