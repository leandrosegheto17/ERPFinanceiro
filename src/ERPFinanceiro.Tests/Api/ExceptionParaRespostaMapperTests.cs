using System;
using System.Net;
using ERPFinanceiro.Api.Erros;
using ERPFinanceiro.Application.Validacao;
using ERPFinanceiro.Domain.Exceptions;
using Xunit;
using ApplicationExceptions = ERPFinanceiro.Application.Exceptions;

namespace ERPFinanceiro.Tests.Api
{
    /// <summary>
    /// Critério de aceite de T-28 (tabela exceção -> HTTP/código, `docs/contrato-v1.1.md`
    /// Seção 2): cada tipo de exceção mapeado ao HTTP e ao `codigo` corretos, e o corpo
    /// do 500 (`ERRO_INTERNO`) nunca contém a mensagem/stack trace da exceção original —
    /// só `DetalheLog` (que vai só para o `IAppLogger`) contém esse detalhe.
    /// </summary>
    public class ExceptionParaRespostaMapperTests
    {
        [Fact]
        public void Mapear_VendaJaCanceladaException_Retorna409VendaJaCancelada()
        {
            var resultado = ExceptionParaRespostaMapper.Mapear(new VendaJaCanceladaException("V-000123"));

            Assert.Equal(HttpStatusCode.Conflict, resultado.StatusHttp);
            Assert.Equal("VENDA_JA_CANCELADA", resultado.Envelope.Erro.Codigo);
            Assert.Contains("V-000123", resultado.Envelope.Erro.Mensagem);
        }

        [Fact]
        public void Mapear_MotivoObrigatorioException_Retorna409MotivoObrigatorio()
        {
            var resultado = ExceptionParaRespostaMapper.Mapear(new MotivoObrigatorioException());

            Assert.Equal(HttpStatusCode.Conflict, resultado.StatusHttp);
            Assert.Equal("MOTIVO_OBRIGATORIO", resultado.Envelope.Erro.Codigo);
        }

        [Fact]
        public void Mapear_VendaNaoEncontradaException_Retorna404VendaNaoEncontrada()
        {
            var resultado = ExceptionParaRespostaMapper.Mapear(new VendaNaoEncontradaException("V-000999"));

            Assert.Equal(HttpStatusCode.NotFound, resultado.StatusHttp);
            Assert.Equal("VENDA_NAO_ENCONTRADA", resultado.Envelope.Erro.Codigo);
            Assert.Contains("V-000999", resultado.Envelope.Erro.Mensagem);
        }

        [Theory]
        [InlineData("PAYLOAD_INVALIDO", "O campo 'itens' não pode ser vazio.")]
        [InlineData("VALOR_TOTAL_DIVERGENTE", "valorTotal diverge da soma dos itens.")]
        public void Mapear_ValidacaoException_Retorna400ComCodigoEspecificoDoErroDeValidacao(string codigo, string mensagem)
        {
            var erro = new ErroValidacao(codigo, mensagem);

            var resultado = ExceptionParaRespostaMapper.Mapear(new ValidacaoException(erro));

            Assert.Equal(HttpStatusCode.BadRequest, resultado.StatusHttp);
            Assert.Equal(codigo, resultado.Envelope.Erro.Codigo);
            Assert.Equal(mensagem, resultado.Envelope.Erro.Mensagem);
        }

        [Theory]
        [InlineData("PAYLOAD_INVALIDO", "O campo 'itens' não pode ser vazio.")]
        [InlineData("VALOR_TOTAL_DIVERGENTE", "valorTotal diverge da soma dos itens.")]
        public void Mapear_ValidacaoExceptionDeApplication_Retorna400ComCodigoEspecificoDoErroDeValidacao(string codigo, string mensagem)
        {
            // T-18: QuitacaoService (Application) lança sua própria ValidacaoException
            // (não a da Api) — ver ExceptionParaRespostaMapper.Mapear.
            var erro = new ErroValidacao(codigo, mensagem);

            var resultado = ExceptionParaRespostaMapper.Mapear(new ApplicationExceptions.ValidacaoException(erro));

            Assert.Equal(HttpStatusCode.BadRequest, resultado.StatusHttp);
            Assert.Equal(codigo, resultado.Envelope.Erro.Codigo);
            Assert.Equal(mensagem, resultado.Envelope.Erro.Mensagem);
        }

        [Fact]
        public void Mapear_DadosDivergentesException_Retorna409DadosDivergentes()
        {
            var resultado = ExceptionParaRespostaMapper.Mapear(new ApplicationExceptions.DadosDivergentesException("V-000123"));

            Assert.Equal(HttpStatusCode.Conflict, resultado.StatusHttp);
            Assert.Equal("DADOS_DIVERGENTES", resultado.Envelope.Erro.Codigo);
            Assert.Contains("V-000123", resultado.Envelope.Erro.Mensagem);
        }

        [Fact]
        public void Mapear_ExcecaoNaoMapeada_Retorna500ErroInternoSemMensagemOriginalNoEnvelope()
        {
            const string mensagemSensivel = "SENHA_BANCO=supersecreta123; connection string real vazou aqui";
            var excecaoOriginal = new InvalidOperationException(mensagemSensivel);

            var resultado = ExceptionParaRespostaMapper.Mapear(excecaoOriginal);

            Assert.Equal(HttpStatusCode.InternalServerError, resultado.StatusHttp);
            Assert.Equal("ERRO_INTERNO", resultado.Envelope.Erro.Codigo);

            // O corpo devolvido ao cliente NUNCA contém a mensagem/stack trace originais.
            Assert.DoesNotContain(mensagemSensivel, resultado.Envelope.Erro.Mensagem);
            Assert.DoesNotContain("InvalidOperationException", resultado.Envelope.Erro.Mensagem);

            // O detalhe completo (para o log interno) contém a mensagem original.
            Assert.Contains(mensagemSensivel, resultado.DetalheLog);
            Assert.Contains("InvalidOperationException", resultado.DetalheLog);
        }

        [Fact]
        public void Mapear_ExcecaoNula_LancaArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ExceptionParaRespostaMapper.Mapear(null));
        }
    }
}
