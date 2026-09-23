namespace ERPFinanceiro.Application.Commands
{
    /// <summary>
    /// Item de venda no payload de entrada (T-13), espelhando <c>docs/contrato-v1.1.md</c>
    /// Seção 3.1 (`itens[].produtoId/quantidade/precoUnitario`). DTO simples de entrada —
    /// validação (quantidade > 0, precoUnitario >= 0) fica em T-17, não aqui.
    /// </summary>
    public class ItemVendaCommand
    {
        public string ProdutoId { get; set; }

        public int Quantidade { get; set; }

        public decimal PrecoUnitario { get; set; }
    }
}
