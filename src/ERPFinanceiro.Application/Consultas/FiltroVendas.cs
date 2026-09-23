using System;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Application.Consultas
{
    /// <summary>
    /// Tipo de filtro para <c>ConsultaService.Listar</c> (T-13). Ainda **sem lógica**
    /// de aplicação do filtro — os filtros só passam a ser efetivamente usados em T-23
    /// (listagem sempre retorna tudo, ordenada por `DataRecebimento` desc) e T-57
    /// (Tier B, aplicação real dos filtros na consulta). Aqui é só o tipo/contrato de
    /// entrada, para não travar a assinatura de `ConsultaService.Listar(FiltroVendas)`.
    /// Todas as propriedades são opcionais (nulo = sem filtro naquele campo).
    /// </summary>
    public class FiltroVendas
    {
        /// <summary>Início do período (`DataRecebimento`, UTC), inclusive. Nulo = sem limite inferior.</summary>
        public DateTime? PeriodoInicioUtc { get; set; }

        /// <summary>Fim do período (`DataRecebimento`, UTC), inclusive. Nulo = sem limite superior.</summary>
        public DateTime? PeriodoFimUtc { get; set; }

        /// <summary>Filtra por `ClienteId`. Nulo/vazio = todos os clientes.</summary>
        public string ClienteId { get; set; }

        /// <summary>Filtra por status. Nulo = todos os status.</summary>
        public StatusVenda? Status { get; set; }
    }
}
