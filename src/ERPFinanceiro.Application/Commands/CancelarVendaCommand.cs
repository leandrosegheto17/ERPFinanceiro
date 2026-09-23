namespace ERPFinanceiro.Application.Commands
{
    /// <summary>
    /// Comando de entrada para cancelamento de venda (T-13), espelhando
    /// <c>docs/contrato-v1.1.md</c> Seção 3.2 (`POST /api/vendas/cancelamento`). Cobre
    /// os três cenários de <c>Venda.Cancelar</c>/<c>Venda.CriarCancelada</c> (Pendente
    /// cancelada; já Cancelada idempotente; desconhecida criada já Cancelada, D-08).
    /// <see cref="Motivo"/> é opcional no payload (obrigatório só quando a venda está
    /// Quitada — regra S-05/D-06, validada em T-17/pelo próprio agregado, não aqui).
    /// </summary>
    public class CancelarVendaCommand
    {
        public string VendaId { get; set; }

        public string Motivo { get; set; }

        /// <summary>Correlation id da requisição (ver <see cref="Interfaces.ICorrelationContext"/>).</summary>
        public string CorrelationId { get; set; }
    }
}
