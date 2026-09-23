using System;

namespace ERPFinanceiro.Application.Exceptions
{
    /// <summary>
    /// Lançada por <c>QuitacaoService</c> (T-18) quando o payload de quitação de uma
    /// venda <c>Pendente</c> diverge dos dados já registrados (<c>clienteId</c>,
    /// <c>valorTotal</c> além da tolerância de 0,01, ou os itens) — I-04 do
    /// <c>PRD-TECNICO.md</c>, D-03, `docs/contrato-v1.1.md` Seção 2/3.1, código
    /// <c>DADOS_DIVERGENTES</c> (409, "A VALIDAR" no contrato, mas já implementável aqui:
    /// o código em si não depende do aceite do lado Vendas, só o *envio* do documento
    /// depende).
    /// </summary>
    public class DadosDivergentesException : Exception
    {
        public string VendaId { get; }

        public DadosDivergentesException(string vendaId)
            : base($"O payload de quitação diverge dos dados registrados na venda Pendente '{vendaId}'.")
        {
            VendaId = vendaId;
        }
    }
}
