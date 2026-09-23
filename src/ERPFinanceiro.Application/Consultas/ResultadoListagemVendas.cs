using System.Collections.Generic;

namespace ERPFinanceiro.Application.Consultas
{
    /// <summary>
    /// Resultado de <c>ConsultaService.Listar</c> (T-23): itens já ordenados por
    /// <c>DataRecebimento</c> desc, limitados a 5.000, mais a flag de truncamento
    /// (<see cref="Truncado"/> = true quando havia mais de 5.000 vendas disponíveis).
    /// </summary>
    public class ResultadoListagemVendas
    {
        public IReadOnlyList<VendaListagemDto> Itens { get; }

        /// <summary>True quando a consulta encontrou mais de 5.000 vendas e a lista foi cortada.</summary>
        public bool Truncado { get; }

        public ResultadoListagemVendas(IReadOnlyList<VendaListagemDto> itens, bool truncado)
        {
            Itens = itens;
            Truncado = truncado;
        }
    }
}
