using System.Collections.Generic;

namespace ERPFinanceiro.Application.Commands
{
    /// <summary>
    /// Comando de entrada para o registro de venda Pendente (T-13), espelhando
    /// <c>docs/contrato-v1.1.md</c> Seção 3.4 (`POST /api/vendas`, aditivo A VALIDAR
    /// D-03/P-9) — mesmo payload de <see cref="QuitarVendaCommand"/>. DTO simples,
    /// sem lógica; o serviço de aplicação (futuro `RegistroService`) é quem valida
    /// (T-17) e traduz para <c>Venda.CriarPendente</c>.
    /// </summary>
    public class RegistrarVendaCommand
    {
        public string VendaId { get; set; }

        public string ClienteId { get; set; }

        public decimal ValorTotal { get; set; }

        public List<ItemVendaCommand> Itens { get; set; }

        /// <summary>Correlation id da requisição (ver <see cref="Interfaces.ICorrelationContext"/>).</summary>
        public string CorrelationId { get; set; }
    }
}
