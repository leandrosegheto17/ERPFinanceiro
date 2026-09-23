using System;
using System.Collections.Generic;
using System.Linq;
using ERPFinanceiro.Application.Commands;
using ERPFinanceiro.Application.Exceptions;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Application.Validacao;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Application.Servicos
{
    /// <summary>
    /// Serviço de aplicação de quitação (T-18, O-05 + S-04; `docs/contrato-v1.1.md`
    /// Seção 3.1; SDD.md Seção 2.2). Orquestra: (1) validação de payload/total (T-17,
    /// <see cref="ValidadorVendaCommand"/>); (2) busca da venda (<see cref="IVendaRepository.ObterPorVendaId"/>);
    /// (3) decisão por estado (inexistente cria+quita — upsert RN-06; Pendente quita,
    /// com checagem de divergência D-03/I-04; Quitada devolve idempotente sem novo
    /// histórico — P-2; Cancelada — <c>Venda.Quitar</c> já lança
    /// <c>VendaJaCanceladaException</c>, T-08); (4) commit via
    /// <see cref="IUnitOfWork.SalvarAlteracoes"/>, dentro de <see cref="ExecutorComRetry"/>
    /// (T-16, regra 9: rollback, reler e reavaliar, máx. 3 tentativas, depois o
    /// conflito propaga para a Api mapear 409 <c>CONFLITO_CONCORRENCIA</c>, T-28).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>RL5-01 (Refatoração Lote-5, resolvida em T-26):</b> cada tentativa de
    /// <see cref="ExecutorComRetry"/> abre seu próprio <see cref="IEscopoOperacao"/>
    /// (via <see cref="IFabricaEscopoOperacao.Abrir"/>) — um <c>DbContext</c> novo por
    /// tentativa, nunca reaproveitado entre elas, dentro do mesmo escopo de
    /// operação/requisição definido pela regra 5 da Seção 1/RT-08 (nunca compartilhado
    /// entre requisições diferentes — só o suficiente para isolar tentativas da mesma
    /// operação). Isso resolve o risco de entidade "zumbi" documentado originalmente
    /// aqui (achado da validação do Lote 5): no ramo "cria" (upsert RN-06), se a 1ª
    /// tentativa falhar por violação de <c>UNIQUE(VENDA_ID)</c> (outra operação
    /// concorrente criou a mesma venda primeiro), a entidade <c>Added</c> daquela
    /// tentativa morre junto com o <c>DbContext</c> descartado (<see cref="IEscopoOperacao.Dispose"/>)
    /// — a 2ª tentativa relê a venda (já criada pela concorrente) num contexto limpo e
    /// resolve como sucesso idempotente, nunca um falso 409 <c>CONFLITO_CONCORRENCIA</c>.
    /// Teste de regressão determinístico (não só estatístico) em
    /// <c>ConcorrenciaIdempotenciaTests</c> (T-20), que força a colisão de
    /// <c>UNIQUE(VENDA_ID)</c> na 1ª tentativa e confirma a 2ª como sucesso.
    /// </para>
    /// </remarks>
    public class QuitacaoService
    {
        private const decimal ToleranciaValorTotal = 0.01m;

        private readonly IFabricaEscopoOperacao _fabricaEscopo;
        private readonly IClock _clock;
        private readonly ICorrelationContext _correlationContext;

        /// <param name="fabricaEscopo">
        /// Fábrica de <see cref="IEscopoOperacao"/> (RL5-01): um novo escopo/<c>DbContext</c>
        /// é aberto a cada tentativa de <see cref="ExecutorComRetry"/> dentro de
        /// <see cref="Executar"/>, nunca compartilhado entre tentativas.
        /// </param>
        public QuitacaoService(
            IFabricaEscopoOperacao fabricaEscopo,
            IClock clock,
            ICorrelationContext correlationContext)
        {
            _fabricaEscopo = fabricaEscopo ?? throw new ArgumentNullException(nameof(fabricaEscopo));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
        }

        /// <summary>
        /// Executa a quitação (contrato-v1.1.md Seção 3.1). Lança
        /// <see cref="ValidacaoException"/> (400) se o payload for inválido/valorTotal
        /// divergir dos itens; <see cref="DadosDivergentesException"/> (409) se a venda
        /// já é Pendente com dados diferentes do payload; <c>VendaJaCanceladaException</c>
        /// (409, Domain) se a venda já está Cancelada; <c>ConcorrenciaException</c> (via
        /// <see cref="ExecutorComRetry"/>, 409 após 3 tentativas) em conflito de
        /// concorrência não resolvido.
        /// </summary>
        public ResultadoQuitacao Executar(QuitarVendaCommand comando)
        {
            var resultadoValidacao = ValidadorVendaCommand.Validar(comando);
            if (!resultadoValidacao.Sucesso)
            {
                // Primeiro erro apenas (decisão documentada em ValidacaoException):
                // o envelope de erro do contrato-v1.1.md Seção 2 carrega um único
                // {codigo,mensagem}, sem lista.
                throw new ValidacaoException(resultadoValidacao.Erros[0]);
            }

            var correlationId = _correlationContext.CorrelationId;

            // RL5-01: um IEscopoOperacao (e portanto um DbContext) novo por tentativa —
            // aberto e descartado dentro do próprio callback, nunca reaproveitado entre
            // as até 3 tentativas de ExecutorComRetry (T-16).
            return ExecutorComRetry.Executar(() =>
            {
                using (var escopo = _fabricaEscopo.Abrir())
                {
                    return ExecutarOperacao(escopo.Repositorio, escopo.UnitOfWork, comando, correlationId);
                }
            });
        }

        private ResultadoQuitacao ExecutarOperacao(
            IVendaRepository repositorio,
            IUnitOfWork unitOfWork,
            QuitarVendaCommand comando,
            string correlationId)
        {
            var vendaExistente = repositorio.ObterPorVendaId(comando.VendaId);

            if (vendaExistente == null)
            {
                return CriarEQuitar(repositorio, unitOfWork, comando, correlationId);
            }

            if (vendaExistente.Status == StatusVenda.Pendente && DivergeDoRegistrado(vendaExistente, comando))
            {
                throw new DadosDivergentesException(vendaExistente.VendaId);
            }

            // Venda.Quitar (T-08) já cobre: Pendente->Quitada (transiciona, novo
            // histórico); já Quitada (idempotente, retorna false, sem novo histórico,
            // P-2); Cancelada (lança VendaJaCanceladaException, mapeada a 409 pela Api).
            var houveTransicao = vendaExistente.Quitar(_clock.UtcNow, correlationId);

            if (houveTransicao)
            {
                unitOfWork.SalvarAlteracoes();
            }

            return new ResultadoQuitacao(vendaExistente.DataQuitacao.Value);
        }

        private ResultadoQuitacao CriarEQuitar(
            IVendaRepository repositorio,
            IUnitOfWork unitOfWork,
            QuitarVendaCommand comando,
            string correlationId)
        {
            var agoraUtc = _clock.UtcNow;

            var itens = comando.Itens
                .Select(item => new VendaItem(item.ProdutoId, item.Quantidade, item.PrecoUnitario))
                .ToList();

            var novaVenda = Venda.CriarPorQuitacao(
                comando.VendaId,
                comando.ClienteId,
                comando.ValorTotal,
                itens,
                dataRecebimentoUtc: agoraUtc,
                dataQuitacaoUtc: agoraUtc,
                correlationId: correlationId);

            repositorio.Adicionar(novaVenda);
            unitOfWork.SalvarAlteracoes();

            return new ResultadoQuitacao(novaVenda.DataQuitacao.Value);
        }

        /// <summary>
        /// D-03/I-04 (contrato-v1.1.md Seções 2/3.1, código <c>DADOS_DIVERGENTES</c>):
        /// compara <c>clienteId</c>, <c>valorTotal</c> (mesma tolerância de 0,01 de
        /// <see cref="ValidadorVendaCommand"/>, RN-02/P-7) e os itens (mesmo conjunto de
        /// produtoId/quantidade/precoUnitario, independente da ordem) do payload contra o
        /// que já está registrado na venda Pendente.
        /// </summary>
        private static bool DivergeDoRegistrado(Venda vendaExistente, QuitarVendaCommand comando)
        {
            if (!string.Equals(vendaExistente.ClienteId, comando.ClienteId, StringComparison.Ordinal))
            {
                return true;
            }

            var valorRegistrado = vendaExistente.ValorTotal ?? 0m;
            var diferenca = valorRegistrado - comando.ValorTotal;
            if (diferenca < 0)
            {
                diferenca = -diferenca;
            }

            if (diferenca > ToleranciaValorTotal)
            {
                return true;
            }

            return ItensDivergem(vendaExistente.Itens, comando.Itens);
        }

        private static bool ItensDivergem(IReadOnlyList<VendaItem> itensRegistrados, List<ItemVendaCommand> itensRecebidos)
        {
            if (itensRegistrados.Count != itensRecebidos.Count)
            {
                return true;
            }

            var registradosOrdenados = itensRegistrados
                .OrderBy(i => i.ProdutoId, StringComparer.Ordinal)
                .ThenBy(i => i.Quantidade)
                .ThenBy(i => i.PrecoUnitario)
                .ToList();

            var recebidosOrdenados = itensRecebidos
                .OrderBy(i => i.ProdutoId, StringComparer.Ordinal)
                .ThenBy(i => i.Quantidade)
                .ThenBy(i => i.PrecoUnitario)
                .ToList();

            for (var i = 0; i < registradosOrdenados.Count; i++)
            {
                var registrado = registradosOrdenados[i];
                var recebido = recebidosOrdenados[i];

                if (!string.Equals(registrado.ProdutoId, recebido.ProdutoId, StringComparison.Ordinal)
                    || registrado.Quantidade != recebido.Quantidade
                    || registrado.PrecoUnitario != recebido.PrecoUnitario)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
