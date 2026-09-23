namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Identificador de correlação da operação em curso (T-13), usado para gravar
    /// <c>CORRELATION_ID</c> no histórico da venda (regra 8) e para <see cref="IAppLogger"/>,
    /// sem que Application precise conhecer OWIN/HTTP. Implementação real fica na Api
    /// (T-25/T-29: lê/gera o header <c>X-Correlation-Id</c> por requisição); aqui só a
    /// interface, para Application depender apenas da abstração.
    /// </summary>
    public interface ICorrelationContext
    {
        /// <summary>Correlation id da requisição/operação atual.</summary>
        string CorrelationId { get; }
    }
}
