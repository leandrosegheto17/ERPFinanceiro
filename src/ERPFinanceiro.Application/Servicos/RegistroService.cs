using System;
using System.Linq;
using ERPFinanceiro.Application.Commands;
using ERPFinanceiro.Application.Exceptions;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Application.Validacao;
using ERPFinanceiro.Domain.Entities;

namespace ERPFinanceiro.Application.Servicos
{
    /// <summary>
    /// Servico de aplicacao de registro de venda Pendente (T-61, RF-04; D-03/P-9;
    /// contrato-v1.1.md Secao 3.4). Valida o payload (T-17), rele a venda e:
    /// inexistente -> cria Pendente (<see cref="Venda.CriarPendente"/>); ja existente
    /// (qualquer status) -> idempotente, devolve o status atual sem alterar nada nem
    /// gravar novo historico. Roda dentro de <see cref="ExecutorComRetry"/> (T-16) com
    /// um <see cref="IEscopoOperacao"/> novo por tentativa (RL5-01), como
    /// <see cref="QuitacaoService"/>: corrida de criacao (UNIQUE(VENDA_ID)) resolve-se
    /// na releitura como sucesso idempotente.
    /// </summary>
    public class RegistroService
    {
        private readonly IFabricaEscopoOperacao _fabricaEscopo;
        private readonly IClock _clock;

        public RegistroService(IFabricaEscopoOperacao fabricaEscopo, IClock clock)
        {
            _fabricaEscopo = fabricaEscopo ?? throw new ArgumentNullException(nameof(fabricaEscopo));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Lanca <see cref="ValidacaoException"/> (400) se o payload for invalido;
        /// <c>ConcorrenciaException</c> se o conflito persistir apos as tentativas.
        /// </summary>
        public ResultadoRegistro Registrar(RegistrarVendaCommand comando)
        {
            var validacao = ValidadorVendaCommand.Validar(comando);
            if (!validacao.Sucesso)
            {
                throw new ValidacaoException(validacao.Erros[0]);
            }

            return ExecutorComRetry.Executar(() =>
            {
                using (var escopo = _fabricaEscopo.Abrir())
                {
                    return RegistrarInterno(escopo.Repositorio, escopo.UnitOfWork, comando);
                }
            });
        }

        private ResultadoRegistro RegistrarInterno(IVendaRepository repositorio, IUnitOfWork unitOfWork, RegistrarVendaCommand comando)
        {
            var venda = repositorio.ObterPorVendaId(comando.VendaId);

            if (venda == null)
            {
                var itens = comando.Itens
                    .Select(i => new VendaItem(i.ProdutoId, i.Quantidade, i.PrecoUnitario))
                    .ToList();

                venda = Venda.CriarPendente(
                    comando.VendaId,
                    comando.ClienteId,
                    comando.ValorTotal,
                    itens,
                    _clock.UtcNow,
                    comando.CorrelationId);

                repositorio.Adicionar(venda);
                unitOfWork.SalvarAlteracoes();
            }

            return new ResultadoRegistro { VendaId = venda.VendaId, Status = venda.Status };
        }
    }
}
