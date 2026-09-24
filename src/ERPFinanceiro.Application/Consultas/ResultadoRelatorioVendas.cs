using System.Collections.Generic;

namespace ERPFinanceiro.Application.Consultas
{
    /// <summary>
    /// Resultado de <c>ConsultaService.ListarParaRelatorio</c> (T-48): mesma listagem de
    /// <c>ConsultaService.Listar</c> (T-23), porém **sem** o limite de 5.000
    /// (<see cref="IVendaConsultaLeitura.ListarTodas"/>, TASK.md Seção 1 regra 15) e com
    /// os agregados que o layout `.frx` (T-49) precisa para o rodapé (UX-SPEC 2.3):
    /// "Total listado" e a contagem de vendas com valor nulo (nota de rodapé,
    /// UX-SPEC 2.3 "Vendas com valor nulo... entram como 0 e são contadas").
    /// </summary>
    public class ResultadoRelatorioVendas
    {
        /// <summary>Linhas do relatório, ordenadas por <c>DataRecebimento</c> desc, sem limite.</summary>
        public IReadOnlyList<LinhaRelatorio> Linhas { get; }

        /// <summary>
        /// Soma de <see cref="LinhaRelatorio.ValorTotal"/> da própria <see cref="Linhas"/>
        /// (nulos tratados como 0) — calculado da mesma lista devolvida, nunca por uma
        /// segunda consulta separada (critério de aceite de T-48). Lista vazia -> 0,00.
        /// </summary>
        public decimal TotalListado { get; }

        /// <summary>
        /// Quantidade de linhas com <see cref="LinhaRelatorio.ValorTotal"/> nulo (venda de
        /// cancelamento desconhecido, ADR-007/D-08) — insumo da nota de rodapé do
        /// relatório (UX-SPEC 2.3: "entram como 0 e são contadas").
        /// </summary>
        public int QuantidadeSemValor { get; }

        public ResultadoRelatorioVendas(IReadOnlyList<LinhaRelatorio> linhas, decimal totalListado, int quantidadeSemValor)
        {
            Linhas = linhas;
            TotalListado = totalListado;
            QuantidadeSemValor = quantidadeSemValor;
        }
    }
}
