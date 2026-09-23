using System;
using ERPFinanceiro.Application.Validacao;

namespace ERPFinanceiro.Application.Exceptions
{
    /// <summary>
    /// Lançada por serviços de aplicação (T-18/T-19, <c>QuitacaoService</c>/
    /// <c>CancelamentoService</c>) quando <see cref="ValidadorVendaCommand"/> (T-17)
    /// devolve <see cref="ResultadoValidacao.Sucesso"/> igual a <c>false</c>. Carrega o
    /// <b>primeiro</b> <see cref="ErroValidacao"/> da lista — decisão adotada aqui
    /// (desvio pequeno, documentado): o contrato v1.1 devolve um único
    /// <c>{erro:{codigo,mensagem}}</c> por resposta (não uma lista), então agregar todos
    /// os erros não teria onde ser exposto sem mudar o contrato; o primeiro erro já é
    /// suficiente para o chamador corrigir e reenviar (o próximo erro, se houver, aparece
    /// na tentativa seguinte). O detalhe completo (todos os <see cref="ResultadoValidacao.Erros"/>)
    /// pode ser registrado em <c>IAppLogger</c> pelo chamador antes de lançar, se desejado.
    /// </summary>
    /// <remarks>
    /// Nota de arquitetura (desvio pequeno relativo à nota de T-28): T-28 documentou o
    /// mecanismo "controller de T-30/T-32 lança <c>ERPFinanceiro.Api.Erros.ValidacaoException</c>
    /// ao ver <c>ResultadoValidacao.Sucesso == false</c>", presumindo que a validação só
    /// aconteceria na borda HTTP. A instrução de T-18 pede que o próprio
    /// <c>QuitacaoService</c> (camada Application) valide e lance. Como
    /// <c>ERPFinanceiro.Application</c> não referencia <c>ERPFinanceiro.Api</c> (SDD 2.1;
    /// <c>ERPFinanceiro.Application.csproj</c> só referencia <c>ERPFinanceiro.Domain</c>),
    /// não é possível lançar o tipo da Api a partir de Application — literalmente não
    /// compilaria. Esta classe espelha a mesma forma (carrega <see cref="ErroValidacao"/>,
    /// mesma semântica de tradução para HTTP 400 com o <c>codigo</c> específico) na camada
    /// onde ela pode de fato ser lançada. <see cref="Api.Erros.ExceptionParaRespostaMapper"/>
    /// (T-28) foi atualizado para mapear também este tipo para 400 — ver nota em
    /// <c>.md/TASK.md</c> (linha T-18) e teste
    /// <c>ExceptionParaRespostaMapperTests.Mapear_ValidacaoExceptionDeApplication_Retorna400</c>.
    /// A classe da Api (<c>ERPFinanceiro.Api.Erros.ValidacaoException</c>) permanece
    /// intacta para uso futuro de T-30/T-32 (ex.: validação adicional só de borda HTTP,
    /// se algum dia existir), sem remoção nem alteração de comportamento.
    /// </remarks>
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
