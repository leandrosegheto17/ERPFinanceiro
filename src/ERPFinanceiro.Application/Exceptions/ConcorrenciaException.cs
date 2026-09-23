using System;

namespace ERPFinanceiro.Application.Exceptions
{
    /// <summary>
    /// Lançada por <see cref="Interfaces.IUnitOfWork.SalvarAlteracoes"/> (implementação
    /// real em Infrastructure, T-15) quando o commit falha por conflito de concorrência:
    /// (1) <c>DbUpdateConcurrencyException</c> do EF6 — 0 linhas afetadas, a <c>VERSAO</c>
    /// lida já foi alterada por outra operação; ou (2) violação de
    /// <c>UNIQUE(VENDA_ID)</c> no Firebird (duas operações concorrentes tentando criar a
    /// mesma venda nova — upsert RN-06, T-18).
    /// <para>
    /// Decisão de local (TASK.md T-15, "decida o local mais coerente com a arquitetura em
    /// camadas"): fica em <c>Application</c>, não em <c>Domain</c> nem em
    /// <c>Infrastructure</c>. Não é <c>DomainException</c> — não é uma regra de negócio do
    /// agregado Venda, é um problema da fronteira transacional (<c>IUnitOfWork</c>, que já
    /// é uma interface de Application). Não pode ficar só em Infrastructure porque quem
    /// precisa capturá-la é o helper de retry de Application (<c>ExecutarComRetry</c>,
    /// T-16, regra 9 do TASK.md Seção 1: rollback, reler e reavaliar, máx. 3 tentativas,
    /// depois 409 <c>CONFLITO_CONCORRENCIA</c>) — e Application nunca referencia
    /// Infrastructure (SDD 2.1), então o tipo lançado por Infrastructure precisa já existir
    /// numa camada que Infrastructure referencia (Application) para ser capturável do lado
    /// de cá da fronteira.
    /// </para>
    /// <para>
    /// Nota: a tradução final para o código de erro público <c>CONFLITO_CONCORRENCIA</c>
    /// (409) é responsabilidade de T-16 (após esgotar as tentativas de retry), não desta
    /// exceção nem de T-15 — aqui ela só sinaliza "o commit atual falhou por conflito,
    /// tente de novo".
    /// </para>
    /// </summary>
    public class ConcorrenciaException : Exception
    {
        public ConcorrenciaException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
