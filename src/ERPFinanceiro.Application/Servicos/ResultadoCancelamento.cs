using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Application.Servicos
{
    /// <summary>
    /// Resultado de <see cref="CancelamentoService.Cancelar"/> (T-19), insumo de
    /// <c>POST /api/vendas/cancelamento</c> (T-32, <c>docs/contrato-v1.1.md</c> Seção 3.2:
    /// <c>200 {"status":"Cancelada"}</c> para os três cenários de sucesso — Pendente
    /// cancelada, já Cancelada idempotente, desconhecida criada já Cancelada). Mesmo
    /// padrão de <c>VendaStatusDto</c> (T-24): <see cref="Status"/> como enum, a
    /// conversão para string PascalCase (P-3) é responsabilidade da Api.
    /// </summary>
    public class ResultadoCancelamento
    {
        public string VendaId { get; set; }

        public StatusVenda Status { get; set; }
    }
}
