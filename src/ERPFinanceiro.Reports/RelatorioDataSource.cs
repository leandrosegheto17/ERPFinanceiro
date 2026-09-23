using System;
using System.Linq;
using ERPFinanceiro.Application.Consultas;

namespace ERPFinanceiro.Reports
{
    /// <summary>
    /// Fonte de dados do relatório (T-48, D-10, ADR-008): usa o mesmo ConsultaService e
    /// FiltroVendas da tela, sem o limite de 5.000; total calculado da mesma lista.
    /// </summary>
    public class RelatorioDataSource
    {
        private readonly ConsultaService _consulta;

        public RelatorioDataSource(ConsultaService consulta)
        {
            _consulta = consulta ?? throw new ArgumentNullException(nameof(consulta));
        }

        public RelatorioDados Obter(FiltroVendas filtro)
        {
            var linhas = _consulta.ListarSemLimite(filtro)
                .Select(v => new LinhaRelatorio
                {
                    VendaId = v.VendaId,
                    ClienteId = v.ClienteId ?? "—",
                    ValorTotal = v.ValorTotal ?? 0m,
                    ValorNulo = !v.ValorTotal.HasValue,
                    Status = v.Status.ToString(),
                    DataRecebimento = v.DataRecebimento,
                    DataQuitacao = v.DataQuitacao,
                    DataCancelamento = v.DataCancelamento
                })
                .ToList();

            return new RelatorioDados
            {
                Linhas = linhas,
                TotalListado = linhas.Sum(l => l.ValorTotal),
                QuantidadeValoresNulos = linhas.Count(l => l.ValorNulo)
            };
        }
    }
}
