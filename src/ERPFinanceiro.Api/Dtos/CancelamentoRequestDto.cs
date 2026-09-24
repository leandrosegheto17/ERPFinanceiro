namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>DTO de entrada de <c>POST /api/vendas/cancelamento</c> (T-32, contrato v1.1 Seção 3.2).</summary>
    public class CancelamentoRequestDto
    {
        public string VendaId { get; set; }

        /// <summary>Opcional; obrigatório só para venda Quitada (D-06), validado no Domain (409 MOTIVO_OBRIGATORIO).</summary>
        public string Motivo { get; set; }
    }
}
