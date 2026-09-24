using System;
using System.Linq;
using ERPFinanceiro.Domain.Entities;
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

        /// <summary>
        /// D-10: <c>false</c> (padrão) = <see cref="ClienteId"/> por igualdade exata;
        /// <c>true</c> = "contém" (substring).
        /// </summary>
        public bool ClienteContem { get; set; }

        /// <summary>Nenhum critério ativo (equivale a "sem filtro").</summary>
        public bool SemCriterios =>
            PeriodoInicioUtc == null && PeriodoFimUtc == null
            && string.IsNullOrWhiteSpace(ClienteId) && Status == null;
    }

    /// <summary>Aplicação (T-57) de <see cref="FiltroVendas"/> sobre uma consulta; traduzível por EF6.</summary>
    public static class FiltroVendasExtensions
    {
        public static IQueryable<Venda> Aplicar(this IQueryable<Venda> consulta, FiltroVendas filtro)
        {
            if (filtro == null) return consulta;
            if (filtro.PeriodoInicioUtc.HasValue)
            {
                DateTime ini = filtro.PeriodoInicioUtc.Value;
                consulta = consulta.Where(v => v.DataRecebimento >= ini);
            }
            if (filtro.PeriodoFimUtc.HasValue)
            {
                DateTime fim = filtro.PeriodoFimUtc.Value;
                consulta = consulta.Where(v => v.DataRecebimento <= fim);
            }
            if (!string.IsNullOrWhiteSpace(filtro.ClienteId))
            {
                string cli = filtro.ClienteId.Trim();
                consulta = filtro.ClienteContem
                    ? consulta.Where(v => v.ClienteId != null && v.ClienteId.Contains(cli))
                    : consulta.Where(v => v.ClienteId == cli);
            }
            if (filtro.Status.HasValue)
            {
                StatusVenda st = filtro.Status.Value;
                consulta = consulta.Where(v => v.Status == st);
            }
            return consulta;
        }
    }
}
