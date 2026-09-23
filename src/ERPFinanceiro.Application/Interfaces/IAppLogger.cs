namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Log interno correlacionável (T-13). Todo detalhe de diagnóstico (inclusive de
    /// exceção) fica só aqui, nunca no envelope de erro devolvido ao cliente
    /// (TASK.md Seção 1, regra 7; `contrato-v1.1.md` Seção 2). <c>ApiKey</c> nunca é
    /// logada (regra 7). Implementação real (T-21, <c>TraceLogger</c>) em
    /// Infrastructure; S-10 (Serilog) é Tier C.
    /// </summary>
    public interface IAppLogger
    {
        /// <summary>
        /// Registra uma linha de log correlacionada. <paramref name="mensagem"/> é o
        /// detalhe interno (pode incluir descrição de exceção) — nunca repassado ao
        /// cliente da API; <paramref name="correlationId"/> é o mesmo identificador
        /// gravado no histórico da venda e devolvido no header <c>X-Correlation-Id</c>
        /// (contrato-v1.1.md Seção 2), permitindo cruzar log com requisição.
        /// </summary>
        void Registrar(string mensagem, string correlationId);
    }
}
