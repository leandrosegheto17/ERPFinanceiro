namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>
    /// DTO de saida de <c>POST /api/vendas</c> (T-62, contrato-v1.1.md Secao 3.4):
    /// <c>{status}</c> em PascalCase (P-3).
    /// </summary>
    public class RegistroResponseDto
    {
        public string Status { get; set; }
    }
}
