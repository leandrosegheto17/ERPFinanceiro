using System;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Application.Consultas
{
    /// <summary>
    /// Linha do relatório financeiro de vendas (T-48/UX-SPEC 2.3: colunas "VendaId |
    /// ClienteId | Valor | Status | Recebida | Quitada | Cancelada"). Estruturalmente
    /// equivalente a <see cref="VendaListagemDto"/> (mesmo cabeçalho de venda, T-23),
    /// mas mantida como tipo próprio de propósito porque é o contrato consumido pelo
    /// layout FastReport `.frx` (T-49, "DTO LinhaRelatorio" — ver TASK.md Seção 4.11) e
    /// pode evoluir de forma independente da grade F-1 (ex.: S-07/T-59-T-60, Tier B).
    /// Nulos de <see cref="ClienteId"/>/<see cref="ValorTotal"/> (venda de cancelamento
    /// desconhecido, ADR-007/D-08) são preservados aqui — decisão de exibir "0,00"/"—"
    /// e a nota de rodapé são de quem consome (<see cref="ResultadoRelatorioVendas"/> já
    /// calcula o total com nulo=0 e a contagem; o layout .frx decide a apresentação).
    /// </summary>
    public class LinhaRelatorio
    {
        public string VendaId { get; set; }

        /// <summary>Anulável (ADR-007/D-08). Preservado como nulo.</summary>
        public string ClienteId { get; set; }

        /// <summary>Anulável (ADR-007/D-08). Preservado como nulo — entra como 0 no "Total listado".</summary>
        public decimal? ValorTotal { get; set; }

        public StatusVenda Status { get; set; }

        public DateTime DataRecebimento { get; set; }

        public DateTime? DataQuitacao { get; set; }

        public DateTime? DataCancelamento { get; set; }
    }
}
