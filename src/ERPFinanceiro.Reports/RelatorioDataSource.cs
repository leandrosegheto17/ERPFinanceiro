using System;
using ERPFinanceiro.Application.Consultas;

namespace ERPFinanceiro.Reports
{
    /// <summary>
    /// Fonte de dados do relatório financeiro de vendas (T-48/UX-SPEC 2.3), consumida
    /// pelo layout FastReport `.frx` (T-49) e pelo fluxo "Emitir relatório" (F-5, T-50).
    /// Camada fina sobre <see cref="ConsultaService"/> (SDD 2.1: `Reports` referencia só
    /// `Application`, nunca EF/Infrastructure diretamente) — reaproveita o **mesmo**
    /// <see cref="ConsultaService"/> e o **mesmo** <see cref="FiltroVendas"/> usados pela
    /// tela (ADR-008: tela e relatório em processo, sem HTTP), mas chama
    /// <see cref="ConsultaService.ListarParaRelatorio"/>, não
    /// <see cref="ConsultaService.Listar"/> — **sem** o limite de 5.000 da grade F-1
    /// (TASK.md Seção 1 regra 15).
    /// <para>
    /// "Total listado" e a contagem de vendas com valor nulo (nota de rodapé, UX-SPEC
    /// 2.3: "Vendas com valor nulo... entram como 0 e são contadas") já vêm calculados
    /// por <see cref="ConsultaService.ListarParaRelatorio"/>, **da mesma lista** —
    /// <see cref="RelatorioDataSource"/> não recalcula nem faz segunda consulta.
    /// </para>
    /// </summary>
    public class RelatorioDataSource
    {
        private readonly ConsultaService _consultaService;

        public RelatorioDataSource(ConsultaService consultaService)
        {
            _consultaService = consultaService ?? throw new ArgumentNullException(nameof(consultaService));
        }

        /// <summary>
        /// Obtém os dados do relatório para o filtro corrente da tela (mesmo
        /// <see cref="FiltroVendas"/> aplicado na grid F-1/F-2, UX-SPEC 2.3). Lista vazia
        /// (nenhuma venda no filtro) devolve <see cref="ResultadoRelatorioVendas.Linhas"/>
        /// vazia e <see cref="ResultadoRelatorioVendas.TotalListado"/> = 0,00 (T-49 decide
        /// a mensagem "Nenhuma venda no período" a partir disso).
        /// </summary>
        public ResultadoRelatorioVendas Obter(FiltroVendas filtro)
        {
            return _consultaService.ListarParaRelatorio(filtro ?? new FiltroVendas());
        }
    }
}
