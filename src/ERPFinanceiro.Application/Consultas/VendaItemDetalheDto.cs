namespace ERPFinanceiro.Application.Consultas
{
    /// <summary>
    /// Item de <see cref="VendaDetalheDto"/> (T-24), com o subtotal já calculado
    /// (Quantidade * PrecoUnitario) — a tela (T-44) não recalcula, só exibe.
    /// </summary>
    public class VendaItemDetalheDto
    {
        public string ProdutoId { get; set; }

        public int Quantidade { get; set; }

        public decimal PrecoUnitario { get; set; }

        public decimal Subtotal { get; set; }
    }
}
