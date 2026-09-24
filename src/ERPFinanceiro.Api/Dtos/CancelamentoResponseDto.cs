namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>
    /// DTO de saída 200 de <c>POST /api/vendas/cancelamento</c> (T-32,
    /// `docs/contrato-v1.1.md` Seção 3.2): <c>{status}</c>. Mapeado manualmente a partir
    /// de <see cref="ERPFinanceiro.Application.Servicos.ResultadoCancelamento"/> pelo
    /// controller. <see cref="Status"/> é string PascalCase (P-3) — os nomes do enum
    /// <see cref="ERPFinanceiro.Domain.Enums.StatusVenda"/> já são PascalCase
    /// (Pendente/Quitada/Cancelada), então <c>ToString()</c> já produz o formato exigido
    /// pelo contrato, sem tabela de conversão adicional.
    /// </summary>
    public class CancelamentoResponseDto
    {
        public string Status { get; set; }
    }
}
