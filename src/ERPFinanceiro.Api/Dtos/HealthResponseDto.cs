namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>Corpo de <c>GET /api/health</c> (contrato v1.1, Seção 3.5). Sem <c>Mensagem</c> de propósito (endpoint público).</summary>
    public class HealthResponseDto
    {
        public string Status { get; set; }
        public string Banco { get; set; }
    }
}
