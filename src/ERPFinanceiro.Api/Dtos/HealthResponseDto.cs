namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>
    /// DTO de saída (200/503) de <c>GET /api/health</c> (T-36, `docs/contrato-v1.1.md`
    /// Seção 3.5): <c>{status,banco}</c>. <c>Status</c> é <c>"ok"</c> (200) ou
    /// <c>"degradado"</c> (503); <c>Banco</c> é <c>"ok"</c> (200) ou <c>"falha"</c>
    /// (503) — campo aditivo em relação ao contrato-base (P-5). Este endpoint não usa
    /// o envelope <c>{erro:{...}}</c> (não é erro de requisição, é status de saúde).
    /// </summary>
    public class HealthResponseDto
    {
        public string Status { get; set; }

        public string Banco { get; set; }
    }
}
