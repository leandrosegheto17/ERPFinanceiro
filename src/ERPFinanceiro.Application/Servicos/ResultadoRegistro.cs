using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Application.Servicos
{
    /// <summary>
    /// Resultado de <see cref="RegistroService.Registrar"/> (T-61), insumo de
    /// <c>POST /api/vendas</c> (T-62): status atual da venda (Pendente no primeiro
    /// registro; o status vigente em repeticoes idempotentes). A conversao para string
    /// PascalCase (P-3) e responsabilidade da Api.
    /// </summary>
    public class ResultadoRegistro
    {
        public string VendaId { get; set; }

        public StatusVenda Status { get; set; }
    }
}
