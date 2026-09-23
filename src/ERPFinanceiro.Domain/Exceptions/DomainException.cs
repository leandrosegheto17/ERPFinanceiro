using System;

namespace ERPFinanceiro.Domain.Exceptions
{
    /// <summary>
    /// Base para exceções de domínio. Nunca deve vazar mensagem/stack trace ao
    /// cliente da API (TASK.md Seção 1, regra 7); a camada Api mapeia o tipo
    /// concreto para o envelope de erro.
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message)
        {
        }
    }
}
