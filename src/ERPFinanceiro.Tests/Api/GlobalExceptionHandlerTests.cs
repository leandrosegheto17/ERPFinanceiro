using System;
using System.Net;
using System.Net.Http;
using ERPFinanceiro.Api.Erros;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Exceptions;
using Moq;
using Xunit;

namespace ERPFinanceiro.Tests.Api
{
    /// <summary>
    /// Critério de aceite de T-28: chama <see cref="GlobalExceptionHandler.Processar"/>
    /// diretamente (mesma lógica usada por <see cref="GlobalExceptionHandler.Handle"/>,
    /// sem precisar subir o host OWIN) para confirmar HTTP/código corretos por tipo de
    /// exceção, que o corpo do 500 não contém a mensagem/stack trace original, e que o
    /// detalhe completo (com a mensagem sensível) vai para <see cref="IAppLogger"/>
    /// correlacionado por <see cref="ICorrelationContext"/>.
    /// </summary>
    public class GlobalExceptionHandlerTests
    {
        [Fact]
        public void Processar_VendaJaCanceladaException_Retorna409ComCodigoCorreto()
        {
            var logger = new Mock<IAppLogger>();
            var correlationContext = new Mock<ICorrelationContext>();
            correlationContext.Setup(c => c.CorrelationId).Returns("corr-123");

            var response = GlobalExceptionHandler.Processar(
                new VendaJaCanceladaException("V-000123"),
                new HttpRequestMessage(),
                logger.Object,
                correlationContext.Object);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var corpo = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("\"codigo\":\"VENDA_JA_CANCELADA\"", corpo);
        }

        [Fact]
        public void Processar_MotivoObrigatorioException_Retorna409ComCodigoCorreto()
        {
            var response = GlobalExceptionHandler.Processar(
                new MotivoObrigatorioException(),
                new HttpRequestMessage(),
                logger: null,
                correlationContext: null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains("\"codigo\":\"MOTIVO_OBRIGATORIO\"", response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public void Processar_VendaNaoEncontradaException_Retorna404ComCodigoCorreto()
        {
            var response = GlobalExceptionHandler.Processar(
                new VendaNaoEncontradaException("V-000999"),
                new HttpRequestMessage(),
                logger: null,
                correlationContext: null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("\"codigo\":\"VENDA_NAO_ENCONTRADA\"", response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public void Processar_ExcecaoNaoMapeada_Retorna500SemStackTraceNoCorpo_EComDetalheNoLogComCorrelationId()
        {
            const string mensagemSensivel = "SENHA_BANCO=supersecreta123";
            var excecaoOriginal = new InvalidOperationException(mensagemSensivel);

            var logger = new Mock<IAppLogger>();
            var correlationContext = new Mock<ICorrelationContext>();
            correlationContext.Setup(c => c.CorrelationId).Returns("corr-abc");

            var response = GlobalExceptionHandler.Processar(
                excecaoOriginal,
                new HttpRequestMessage(),
                logger.Object,
                correlationContext.Object);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var corpo = response.Content.ReadAsStringAsync().Result;

            // Corpo devolvido ao cliente: código genérico, nunca a mensagem/stack trace originais.
            Assert.Contains("\"codigo\":\"ERRO_INTERNO\"", corpo);
            Assert.DoesNotContain(mensagemSensivel, corpo);
            Assert.DoesNotContain("InvalidOperationException", corpo);

            // Log interno: recebe o detalhe completo (com a mensagem sensível) e o correlationId.
            logger.Verify(l => l.Registrar(
                It.Is<string>(msg => msg.Contains(mensagemSensivel) && msg.Contains("InvalidOperationException")),
                "corr-abc"),
                Times.Once);
        }

        [Fact]
        public void Processar_SemLoggerResolvido_NaoLancaEDevolveRespostaMesmoAssim()
        {
            var response = GlobalExceptionHandler.Processar(
                new InvalidOperationException("qualquer coisa"),
                new HttpRequestMessage(),
                logger: null,
                correlationContext: null);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
