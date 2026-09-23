using System;
using System.Net;
using ERPFinanceiro.Domain.Exceptions;
using ApplicationExceptions = ERPFinanceiro.Application.Exceptions;

namespace ERPFinanceiro.Api.Erros
{
    /// <summary>
    /// Resultado da tradução de uma exceção para resposta HTTP (T-28): status,
    /// envelope a devolver ao cliente e o detalhe completo (tipo + mensagem +
    /// stack trace) a gravar só no <c>IAppLogger</c> — nunca no envelope.
    /// </summary>
    public class RespostaErroMapeada
    {
        public RespostaErroMapeada(HttpStatusCode statusHttp, ErroEnvelopeDto envelope, string detalheLog)
        {
            StatusHttp = statusHttp;
            Envelope = envelope;
            DetalheLog = detalheLog;
        }

        public HttpStatusCode StatusHttp { get; }

        public ErroEnvelopeDto Envelope { get; }

        public string DetalheLog { get; }
    }

    /// <summary>
    /// Tabela exceção -> HTTP/código do envelope de erro (T-28, `docs/contrato-v1.1.md`
    /// Seção 2, `TASK.md` Seção 1 regra 7). Classe pura (sem Web API/OWIN), testável
    /// diretamente por xUnit sem subir host — <see cref="GlobalExceptionHandler"/> é só
    /// o adaptador que conecta isto ao pipeline e ao log.
    /// </summary>
    public static class ExceptionParaRespostaMapper
    {
        private const string MensagemGenericaErroInterno =
            "Ocorreu um erro interno inesperado. Consulte o suporte informando o horário da operação.";

        public static RespostaErroMapeada Mapear(Exception excecao)
        {
            if (excecao == null)
            {
                throw new ArgumentNullException(nameof(excecao));
            }

            switch (excecao)
            {
                case VendaJaCanceladaException vendaJaCancelada:
                    return Envelope(HttpStatusCode.Conflict, "VENDA_JA_CANCELADA", vendaJaCancelada.Message, vendaJaCancelada);

                case MotivoObrigatorioException motivoObrigatorio:
                    return Envelope(HttpStatusCode.Conflict, "MOTIVO_OBRIGATORIO", motivoObrigatorio.Message, motivoObrigatorio);

                case VendaNaoEncontradaException vendaNaoEncontrada:
                    return Envelope(HttpStatusCode.NotFound, "VENDA_NAO_ENCONTRADA", vendaNaoEncontrada.Message, vendaNaoEncontrada);

                case ValidacaoException validacao:
                    return Envelope(HttpStatusCode.BadRequest, validacao.Erro.Codigo, validacao.Erro.Mensagem, validacao);

                // T-18: QuitacaoService (Application) valida e lança sua própria
                // ValidacaoException (não pode lançar o tipo acima, que vive na Api —
                // ERPFinanceiro.Application não referencia ERPFinanceiro.Api, SDD 2.1;
                // ver remarks de ERPFinanceiro.Application.Exceptions.ValidacaoException).
                // Mesma tradução: 400 com o código/mensagem específicos do ErroValidacao.
                case ApplicationExceptions.ValidacaoException validacaoApplication:
                    return Envelope(HttpStatusCode.BadRequest, validacaoApplication.Erro.Codigo, validacaoApplication.Erro.Mensagem, validacaoApplication);

                // T-18 (ramo Pendente, D-03/I-04): payload de quitação diverge dos
                // dados já registrados na venda Pendente. Código novo do contrato v1.1
                // (Seção 2/3.1), "A VALIDAR" quanto ao aceite formal do lado Vendas, mas
                // já implementável (o aceite pendente é sobre o *envio* do documento,
                // não sobre a Api poder usar o código).
                case ApplicationExceptions.DadosDivergentesException dadosDivergentes:
                    return Envelope(HttpStatusCode.Conflict, "DADOS_DIVERGENTES", dadosDivergentes.Message, dadosDivergentes);

                default:
                    // Qualquer exceção não mapeada: 500 ERRO_INTERNO com mensagem
                    // genérica ao cliente — a mensagem/stack trace real da exceção
                    // original só vai para DetalheLog (regra 7).
                    return Envelope(HttpStatusCode.InternalServerError, "ERRO_INTERNO", MensagemGenericaErroInterno, excecao);
            }
        }

        private static RespostaErroMapeada Envelope(HttpStatusCode status, string codigo, string mensagemCliente, Exception original)
        {
            var envelope = new ErroEnvelopeDto
            {
                Erro = new ErroDto { Codigo = codigo, Mensagem = mensagemCliente }
            };

            var detalheLog = $"{original.GetType().FullName}: {original.Message}{Environment.NewLine}{original.StackTrace}";

            return new RespostaErroMapeada(status, envelope, detalheLog);
        }
    }
}
