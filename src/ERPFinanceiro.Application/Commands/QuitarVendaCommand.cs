using System.Collections.Generic;

namespace ERPFinanceiro.Application.Commands
{
    /// <summary>
    /// Comando de entrada para quitação de venda (T-13), espelhando
    /// <c>docs/contrato-v1.1.md</c> Seção 3.1 (`POST /api/vendas/quitacao`). Cobre os
    /// três cenários de <c>Venda.CriarPorQuitacao</c>/<c>Venda.Quitar</c> (venda
    /// inexistente criada e quitada; Pendente quitada; já Quitada idempotente). DTO
    /// simples de entrada — validação de payload/total fica em T-17 (`QuitarVendaService`
    /// futuro).
    /// </summary>
    public class QuitarVendaCommand
    {
        public string VendaId { get; set; }

        public string ClienteId { get; set; }

        public decimal ValorTotal { get; set; }

        public List<ItemVendaCommand> Itens { get; set; }

        /// <summary>Correlation id da requisição (ver <see cref="Interfaces.ICorrelationContext"/>).</summary>
        public string CorrelationId { get; set; }
    }
}
