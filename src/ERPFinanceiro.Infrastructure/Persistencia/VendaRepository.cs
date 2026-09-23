using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;

namespace ERPFinanceiro.Infrastructure.Persistencia
{
    /// <summary>
    /// Implementação EF6 (T-14) de <see cref="IVendaRepository"/> contra
    /// <see cref="FinanceiroDbContext"/> (T-11). Só busca/inserção via
    /// <see cref="IVendaRepository"/> — mutação de venda existente é feita pelo próprio
    /// agregado (Quitar/Cancelar) mais <see cref="IUnitOfWork.SalvarAlteracoes"/>
    /// (regra 8, ver comentário da interface). O commit real (<c>SaveChanges</c>) fica
    /// com <see cref="IUnitOfWork"/> (T-15); este repositório apenas registra a intenção
    /// no <see cref="DbContext"/> (Add) ou lê.
    ///
    /// Também implementa <see cref="IVendaConsultaLeitura"/> (T-23): interface de leitura
    /// separada de <see cref="IVendaRepository"/> (cujo contrato está travado em 2
    /// métodos por teste estrutural, T-13) — mesma classe concreta, duas interfaces,
    /// consumidas por <c>ConsultaService</c> (Application), que não pode referenciar
    /// EF/DbContext diretamente (SDD 2.1).
    /// </summary>
    public class VendaRepository : IVendaRepository, IVendaConsultaLeitura
    {
        private readonly FinanceiroDbContext _contexto;

        public VendaRepository(FinanceiroDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <inheritdoc />
        public Venda ObterPorVendaId(string vendaId)
        {
            // Busca por VendaId (identificador de negócio), não pela PK técnica Id.
            // Include por nome de string: ItensEf/HistoricoEf são propriedades internal
            // de uso exclusivo do mapeamento EF (ver Venda.cs/FinanceiroDbContextTests) —
            // o overload baseado em string resolve o caminho de navegação pelo metadado
            // do modelo sem exigir acesso compile-time ao membro internal.
            //
            // Sem AsNoTracking aqui de propósito: quem chama ObterPorVendaId normalmente
            // segue com uma mutação do agregado (Quitar/Cancelar) na mesma unidade de
            // trabalho, e precisa do tracking do EF para o SaveChanges (T-15) detectar a
            // mudança de VERSAO/STATUS. Consulta somente-leitura fica em
            // ObterParaConsultaSomenteLeitura (reaproveitável por T-23/ConsultaService).
            return _contexto.Vendas
                .Include("ItensEf")
                .Include("HistoricoEf")
                .SingleOrDefault(v => v.VendaId == vendaId);
        }

        /// <inheritdoc />
        public void Adicionar(Venda venda)
        {
            // Registra o agregado novo (venda + itens + primeira entrada de histórico,
            // já compostos pelas fábricas de Venda) no contexto. O commit/transação real
            // fica a cargo de IUnitOfWork.SalvarAlteracoes (T-15) — aqui só marca para
            // inserção (EF6 rastreia o grafo completo a partir da raiz).
            _contexto.Vendas.Add(venda);
        }

        /// <summary>
        /// Consulta de leitura sem tracking, por VendaId, incluindo itens/histórico.
        /// Não faz parte de <see cref="IVendaRepository"/> (contrato deliberadamente restrito
        /// a busca/inserção, ver comentário da interface) — método auxiliar deste
        /// repositório concreto, pensado para ser reaproveitado por
        /// <c>ConsultaService</c> (T-23/T-24), que também precisa de leitura AsNoTracking
        /// sem pagar o custo de tracking do EF numa operação puramente de leitura.
        /// </summary>
        public Venda ObterParaConsultaSomenteLeitura(string vendaId)
        {
            return _contexto.Vendas
                .Include("ItensEf")
                .Include("HistoricoEf")
                .AsNoTracking()
                .SingleOrDefault(v => v.VendaId == vendaId);
        }

        /// <inheritdoc cref="IVendaConsultaLeitura.ListarMaisRecentes" />
        /// <summary>
        /// Consulta de listagem (T-23): sem tracking, ordenada por <c>DataRecebimento</c>
        /// desc, limitada a <paramref name="quantidadeMaxima"/> no próprio banco (Take),
        /// para não trazer a tabela inteira para memória. Não inclui Itens/Historico —
        /// a grade de listagem (UX-SPEC 2.1) não usa esses dados; quem precisa do
        /// detalhe completo é <c>ObterParaConsultaSomenteLeitura</c>/<c>ObterPorVendaId</c>
        /// (T-24, por venda).
        /// </summary>
        public IReadOnlyList<Venda> ListarMaisRecentes(int quantidadeMaxima)
        {
            return _contexto.Vendas
                .AsNoTracking()
                .OrderByDescending(v => v.DataRecebimento)
                .Take(quantidadeMaxima)
                .ToList();
        }
    }
}
