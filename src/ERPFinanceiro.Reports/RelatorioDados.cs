using System.Collections.Generic;

namespace ERPFinanceiro.Reports
{
    /// <summary>Conjunto de dados do relatório: linhas + "Total listado" da mesma lista + nulos para nota de rodapé.</summary>
    public class RelatorioDados
    {
        public IReadOnlyList<LinhaRelatorio> Linhas { get; set; }

        /// <summary>Soma de ValorTotal das Linhas (nulos = 0); 0,00 se vazia.</summary>
        public decimal TotalListado { get; set; }

        /// <summary>Quantidade de linhas com valor nulo tratado como 0.</summary>
        public int QuantidadeValoresNulos { get; set; }

        public string NotaRodape =>
            QuantidadeValoresNulos > 0
                ? QuantidadeValoresNulos + " venda(s) com valor desconhecido considerada(s) como 0,00."
                : string.Empty;
    }
}
