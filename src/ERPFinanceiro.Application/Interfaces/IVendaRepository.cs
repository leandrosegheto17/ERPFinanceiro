using ERPFinanceiro.Domain.Entities;

namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Persistência do agregado <see cref="Venda"/> (T-13). Expõe deliberadamente só
    /// busca e inserção: histórico é append-only (TASK.md Seção 1, regra 8) e toda
    /// mutação de uma venda já existente acontece através do próprio agregado
    /// (<see cref="Venda.Quitar"/>/<see cref="Venda.Cancelar"/>) seguido de
    /// <see cref="IUnitOfWork.SalvarAlteracoes"/> — nunca por um método de
    /// "Atualizar"/"Update" genérico neste repositório, que reabriria a porta para
    /// sobrescrever histórico ou pular a mutação controlada pelo domínio. Forma travada
    /// por teste estrutural (<c>ApplicationContratosTests.IVendaRepository_NaoExpoeMetodoDeAtualizacaoGenerica</c>,
    /// T-13): exatamente 2 métodos. Leitura de listagem (T-23) fica em
    /// <see cref="IVendaConsultaLeitura"/>, interface separada, para não violar esse
    /// contrato já fixado.
    /// Implementação real (EF6, `.fdb`) em T-14 (Infrastructure).
    /// </summary>
    public interface IVendaRepository
    {
        /// <summary>
        /// Busca a venda pelo identificador de negócio (`VendaId`, não a PK técnica
        /// `Id`), incluindo itens e histórico. Retorna <c>null</c> quando a venda não
        /// existe — nunca lança para "não encontrado" (quem decide 404 vs. outro
        /// comportamento é o serviço de aplicação/controller, não o repositório).
        /// </summary>
        Venda ObterPorVendaId(string vendaId);

        /// <summary>
        /// Registra uma venda nova (agregado + itens + primeira entrada de histórico,
        /// já criados pelas fábricas de <see cref="Venda"/>). Não persiste sozinho —
        /// a transação/commit fica a cargo de <see cref="IUnitOfWork.SalvarAlteracoes"/>,
        /// chamado pelo serviço de aplicação depois de compor a operação completa
        /// (Venda + itens + histórico na mesma transação, regra 8).
        /// </summary>
        void Adicionar(Venda venda);
    }
}
