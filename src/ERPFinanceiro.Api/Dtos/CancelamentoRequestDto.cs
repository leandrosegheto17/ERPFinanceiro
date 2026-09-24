namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>
    /// DTO de entrada de <c>POST /api/vendas/cancelamento</c> (T-32, `docs/contrato-v1.1.md`
    /// Seção 3.2): espelha o JSON de entrada campo a campo (o formatter JSON da Api,
    /// <see cref="Startup.CriarConfiguracaoJson"/>, converte para/de camelCase). Nunca é a
    /// entidade de Domain — o controller mapeia manualmente para
    /// <see cref="ERPFinanceiro.Application.Commands.CancelarVendaCommand"/> (Seção 1, regra 2).
    /// </summary>
    public class CancelamentoRequestDto
    {
        public string VendaId { get; set; }

        /// <summary>
        /// Opcional no payload — obrigatório apenas quando a venda está Quitada (D-06),
        /// regra aplicada pelo próprio agregado (<c>Venda.Cancelar</c>, T-09), não aqui.
        /// </summary>
        public string Motivo { get; set; }
    }
}
