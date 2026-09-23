using System;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Application.Consultas
{
    /// <summary>
    /// DTO de listagem para <c>ConsultaService.Listar</c> (T-23). Espelha as colunas da
    /// grade F-1 (UX-SPEC 2.1). Nulos de <see cref="ClienteId"/>/<see cref="ValorTotal"/>
    /// (venda de cancelamento desconhecido, ADR-007/D-08) são preservados aqui — a
    /// decisão de exibir "—" é da camada de apresentação (T-42/T-48), não deste DTO.
    /// </summary>
    public class VendaListagemDto
    {
        public string VendaId { get; set; }

        /// <summary>Anulável (ADR-007/D-08). Preservado como nulo; FE decide exibir "—".</summary>
        public string ClienteId { get; set; }

        /// <summary>Anulável (ADR-007/D-08). Preservado como nulo; FE decide exibir "—".</summary>
        public decimal? ValorTotal { get; set; }

        public StatusVenda Status { get; set; }

        public DateTime DataRecebimento { get; set; }

        public DateTime? DataQuitacao { get; set; }

        public DateTime? DataCancelamento { get; set; }
    }
}
