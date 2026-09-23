using System;
using ERPFinanceiro.Application.Validacao;

namespace ERPFinanceiro.Api.Erros
{
    /// <summary>
    /// Mecanismo escolhido em T-28 para propagar um erro de validação (T-17,
    /// <c>ValidadorVendaCommand</c>) até o <c>GlobalExceptionHandler</c>: T-17
    /// documenta explicitamente que <c>ResultadoValidacao</c> nunca lança exceção
    /// (Application inspeciona <c>Sucesso</c>/<c>Erros</c>); é o controller (Api,
    /// T-30/T-32, ainda não implementados) quem, ao ver
    /// <c>ResultadoValidacao.Sucesso == false</c>, lança esta exceção carregando o
    /// primeiro <see cref="ErroValidacao"/> (código específico, ex.
    /// <c>PAYLOAD_INVALIDO</c>/<c>VALOR_TOTAL_DIVERGENTE</c> — nunca um 400
    /// genérico). O handler global mapeia para HTTP 400 com esse código/mensagem.
    /// </summary>
    public class ValidacaoException : Exception
    {
        public ErroValidacao Erro { get; }

        public ValidacaoException(ErroValidacao erro)
            : base(erro?.Mensagem ?? "Erro de validação.")
        {
            Erro = erro ?? throw new ArgumentNullException(nameof(erro));
        }
    }
}
