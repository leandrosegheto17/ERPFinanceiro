using System.Runtime.Remoting.Messaging;
using ERPFinanceiro.Application.Interfaces;

namespace ERPFinanceiro.Api
{
    /// <summary>
    /// Implementação concreta (T-25) de <see cref="ICorrelationContext"/> para a Api.
    /// Hospedagem é <c>Microsoft.Owin.Host.HttpListener</c> (ADR-004) — OWIN "puro",
    /// sem System.Web: <c>HttpContext.Current</c> não é populado. Por isso o valor
    /// atual fica em <see cref="CallContext"/> (logical call context), que atravessa
    /// os <c>await</c> da mesma requisição sem depender de System.Web.
    /// <para>
    /// Quem popula <see cref="CorrelationId"/> a cada requisição é o
    /// <c>CorrelationIdHandler</c> (T-29, ainda não implementado) — ele lê/gera o
    /// header <c>X-Correlation-Id</c> e chama <see cref="Definir"/> no início do
    /// pipeline. Esta classe, por si, não gera nem lê nenhum header.
    /// </para>
    /// </summary>
    public class CorrelationContext : ICorrelationContext
    {
        private const string ChaveCallContext = "ERPFinanceiro.CorrelationId";

        public string CorrelationId => CallContext.LogicalGetData(ChaveCallContext) as string;

        /// <summary>
        /// Define o correlation id da requisição/operação atual. Chamado pelo
        /// <c>CorrelationIdHandler</c> (T-29) uma vez por requisição, antes de o
        /// controller/serviço de aplicação rodar.
        /// </summary>
        public void Definir(string correlationId)
        {
            CallContext.LogicalSetData(ChaveCallContext, correlationId);
        }
    }
}
