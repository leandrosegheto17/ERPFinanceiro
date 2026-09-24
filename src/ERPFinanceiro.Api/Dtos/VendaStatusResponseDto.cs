namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>
    /// DTO de saída 200 de <c>GET /api/vendas/{vendaId}/status</c> (T-31, `docs/contrato-v1.1.md`
    /// Seção 3.3): <c>{vendaId,status}</c>, status como string PascalCase (P-3).
    /// </summary>
    public class VendaStatusResponseDto
    {
        public string VendaId { get; set; }

        public string Status { get; set; }
    }
}
