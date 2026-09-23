using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using ERPFinanceiro.Application.Interfaces;

namespace ERPFinanceiro.Api.Erros
{
    /// <summary>
    /// Handler global de exceção (T-28, TASK.md Seção 1 regra 7): registrado em
    /// <see cref="Startup"/> via <c>config.Services.Replace(typeof(IExceptionHandler), ...)</c>,
    /// traduz qualquer exceção não tratada pelo controller para o envelope
    /// <c>{erro:{codigo,mensagem}}</c> (<see cref="ExceptionParaRespostaMapper"/>) e grava o
    /// detalhe completo (nunca exposto ao cliente) em <see cref="IAppLogger"/>,
    /// correlacionado por <see cref="ICorrelationContext"/>.
    /// <para>
    /// <see cref="IAppLogger"/>/<see cref="ICorrelationContext"/> são resolvidos por
    /// requisição via <see cref="HttpRequestMessage.GetDependencyScope"/> (Autofac,
    /// escopo por requisição registrado no composition root do Desktop, T-26) — em vez
    /// de injeção via construtor — para não acoplar a ordem de inicialização entre T-26
    /// (Desktop) e T-28/T-29 (Api), tarefas paralelas do mesmo lote. Se a resolução
    /// falhar (ex. container ainda sem essas interfaces registradas), o handler ainda
    /// devolve o envelope correto ao cliente — só o log interno fica ausente.
    /// </para>
    /// </summary>
    public class GlobalExceptionHandler : ExceptionHandler
    {
        public override void Handle(ExceptionHandlerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.Request;
            var logger = ResolverServico<IAppLogger>(request);
            var correlationContext = ResolverServico<ICorrelationContext>(request);

            var response = Processar(context.Exception, request, logger, correlationContext);
            context.Result = new ResponseMessageResult(response);
        }

        /// <summary>
        /// Lógica testável do handler (usada pelos testes de T-28 chamando-a
        /// diretamente, sem subir host OWIN): mapeia a exceção para HTTP/envelope e
        /// grava o detalhe completo (com mensagem/stack trace originais) no log,
        /// nunca no corpo da resposta.
        /// </summary>
        public static HttpResponseMessage Processar(
            Exception excecao,
            HttpRequestMessage request,
            IAppLogger logger,
            ICorrelationContext correlationContext)
        {
            var mapeado = ExceptionParaRespostaMapper.Mapear(excecao);

            logger?.Registrar(mapeado.DetalheLog, correlationContext?.CorrelationId);

            var formatter = new JsonMediaTypeFormatter
            {
                SerializerSettings = Startup.CriarConfiguracaoJson()
            };

            return new HttpResponseMessage(mapeado.StatusHttp)
            {
                Content = new ObjectContent<ErroEnvelopeDto>(mapeado.Envelope, formatter),
                RequestMessage = request
            };
        }

        private static T ResolverServico<T>(HttpRequestMessage request) where T : class
        {
            var scope = request?.GetDependencyScope();
            return scope?.GetService(typeof(T)) as T;
        }
    }
}
