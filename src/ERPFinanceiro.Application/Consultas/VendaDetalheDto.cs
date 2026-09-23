using System;
using System.Collections.Generic;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Application.Consultas
{
    /// <summary>
    /// DTO de saída de <c>ConsultaService.ObterDetalhe</c> (T-24). Cabeçalho + itens
    /// com subtotal calculado. Deliberadamente **sem** histórico (RF-03/Tier C: a
    /// aba de histórico não faz parte da tela de detalhe; o histórico continua sendo
    /// carregado pelo agregado <c>Venda</c> via <see cref="Interfaces.IVendaRepository.ObterPorVendaId"/>,
    /// só não é exposto aqui).
    /// </summary>
    public class VendaDetalheDto
    {
        public string VendaId { get; set; }

        /// <summary>Anulável — venda de cancelamento de venda desconhecida (D-08/ADR-007) não tem cliente.</summary>
        public string ClienteId { get; set; }

        /// <summary>Anulável — mesma razão de <see cref="ClienteId"/>.</summary>
        public decimal? ValorTotal { get; set; }

        public StatusVenda Status { get; set; }

        public DateTime DataRecebimento { get; set; }

        public DateTime? DataQuitacao { get; set; }

        public DateTime? DataCancelamento { get; set; }

        public string MotivoCancelamento { get; set; }

        /// <summary>Itens com subtotal calculado (Quantidade * PrecoUnitario). Vazia para venda cancelada sem itens (D-08/ADR-007) — não é erro.</summary>
        public IReadOnlyList<VendaItemDetalheDto> Itens { get; set; }

        /// <summary>Soma dos subtotais dos itens. 0 quando <see cref="Itens"/> está vazia.</summary>
        public decimal TotalItens { get; set; }
    }
}
