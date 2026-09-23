using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Application.Consultas
{
    /// <summary>
    /// DTO enxuto de saída de <c>ConsultaService.ObterStatus</c> (T-24) — insumo do
    /// futuro <c>GET /api/vendas/{vendaId}/status</c> (T-31).
    /// </summary>
    public class VendaStatusDto
    {
        public string VendaId { get; set; }

        public StatusVenda Status { get; set; }
    }
}
