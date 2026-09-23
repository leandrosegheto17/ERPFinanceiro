using System;

namespace ERPFinanceiro.Reports
{
    /// <summary>DTO de linha do relatório (T-48; consumido pelo layout .frx de T-49). Nulos já tratados como 0.</summary>
    public class LinhaRelatorio
    {
        public string VendaId { get; set; }

        /// <summary>Nulo do banco vira "—" (UX-SPEC 7).</summary>
        public string ClienteId { get; set; }

        /// <summary>Nulo do banco vira 0 (CA-07.3).</summary>
        public decimal ValorTotal { get; set; }

        /// <summary>True quando o ValorTotal original era nulo (venda desconhecida, ADR-007/D-08).</summary>
        public bool ValorNulo { get; set; }

        public string Status { get; set; }

        public DateTime DataRecebimento { get; set; }
        public DateTime? DataQuitacao { get; set; }
        public DateTime? DataCancelamento { get; set; }
    }
}
