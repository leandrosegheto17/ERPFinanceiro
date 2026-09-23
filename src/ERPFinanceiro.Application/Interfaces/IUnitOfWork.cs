namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Fronteira transacional entre Application e a persistência real (T-13/T-11).
    /// Um único <see cref="SalvarAlteracoes"/> por operação de negócio garante Venda +
    /// itens + histórico na mesma transação (TASK.md Seção 1, regra 8).
    ///
    /// Decisão: método **síncrono**, não <c>Task SalvarAlteracoesAsync()</c>. A regra 5
    /// do TASK.md Seção 1 proíbe <c>async void</c> e exige que a UI nunca bloqueie a
    /// thread — mas isso é responsabilidade de quem *chama* a Application (Api
    /// controller/Desktop), não da própria Application, que pode continuar síncrona
    /// (SDD 2.1: Application não referencia EF/Web API). O I/O real de rede/disco fica
    /// inteiramente em Infrastructure (EF6 `SaveChanges`); a chamada assíncrona, quando
    /// necessária (ex.: controller Web API), envolve esta chamada síncrona num
    /// <c>Task.Run</c>/await na borda apropriada, sem forçar a interface de Application a
    /// carregar `System.Threading.Tasks` como parte do contrato de domínio de aplicação.
    /// Reavaliar apenas se um cenário concreto de Application (não Infrastructure) exigir
    /// I/O assíncrono direto — não é o caso hoje.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Confirma (commit) todas as alterações pendentes da operação de negócio atual
        /// numa única transação. Implementação real (EF6 <c>SaveChanges</c> + tratamento
        /// de conflito de <c>VERSAO</c>, regra 9) em Infrastructure (T-11/T-14).
        /// </summary>
        void SalvarAlteracoes();
    }
}
