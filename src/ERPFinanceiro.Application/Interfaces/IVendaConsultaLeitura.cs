using System.Collections.Generic;
using ERPFinanceiro.Domain.Entities;

namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Leitura de vendas para listagem/consulta (T-23), separada deliberadamente de
    /// <see cref="IVendaRepository"/> (contrato restrito a busca-por-id/inserção, forma
    /// travada por teste estrutural — ver comentário da própria interface). Application
    /// não referencia EF/DbContext diretamente (SDD 2.1, TASK.md Seção 1 regra 1) — esta
    /// interface é o ponto de extensão que permite a <c>ConsultaService</c> (Application)
    /// consultar o banco sem conhecer EF6/Firebird; a implementação real fica em
    /// Infrastructure (<c>VendaRepository</c>, reaproveitando o mesmo padrão AsNoTracking
    /// de <c>ObterParaConsultaSomenteLeitura</c>, T-14).
    /// </summary>
    public interface IVendaConsultaLeitura
    {
        /// <summary>
        /// Lista até <paramref name="quantidadeMaxima"/> vendas, ordenadas por
        /// <c>DataRecebimento</c> desc, sem tracking. Não aplica nenhum filtro de
        /// <c>FiltroVendas</c> (T-57, Tier B) — decisão de quais/quantas linhas pedir
        /// (inclusive o "+1" para detectar truncamento) é de quem chama
        /// (<c>ConsultaService.Listar</c>, T-23).
        /// </summary>
        IReadOnlyList<Venda> ListarMaisRecentes(int quantidadeMaxima);

        /// <summary>
        /// Lista **todas** as vendas, ordenadas por <c>DataRecebimento</c> desc, sem
        /// tracking e **sem limite** (T-48/TASK.md Seção 1 regra 15: "o relatório usa o
        /// mesmo filtro e sem o limite de 5.000" — ao contrário de
        /// <see cref="ListarMaisRecentes"/>, usado pela grade F-1/T-23). Não aplica
        /// nenhum filtro de <c>FiltroVendas</c> (T-57, Tier B, mesma decisão já tomada
        /// para <see cref="ListarMaisRecentes"/>) — quem chama é
        /// <c>ConsultaService.ListarParaRelatorio</c> (T-48).
        /// </summary>
        IReadOnlyList<Venda> ListarTodas();
    }
}
