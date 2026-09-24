namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>
    /// DTO de saída 200 de <c>GET /api/vendas/{vendaId}/status</c> (T-31,
    /// `docs/contrato-v1.1.md`): <c>{vendaId,status}</c>. <see cref="Status"/> é o
    /// <c>enum</c> <see cref="ERPFinanceiro.Domain.Enums.StatusVenda"/> convertido para
    /// string PascalCase (<c>Enum.ToString()</c>, valores já PascalCase: Pendente/
    /// Quitada/Cancelada) pelo controller — mesma decisão de T-19/T-24 de manter a
    /// conversão enum-&gt;string na camada Api, não em Domain/Application.
    /// </summary>
    public class VendaStatusResponseDto
    {
        public string VendaId { get; set; }

        public string Status { get; set; }
    }
}
