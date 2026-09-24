namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>DTO de saída 200 de <c>POST /api/vendas/cancelamento</c> (T-32): <c>{status}</c>, PascalCase (P-3).</summary>
    public class CancelamentoResponseDto
    {
        public string Status { get; set; }
    }
}
