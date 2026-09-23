namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Fábrica de <see cref="IEscopoOperacao"/> — cada chamada a <see cref="Abrir"/>
    /// deve produzir um <see cref="IEscopoOperacao"/> novo, com um <c>DbContext</c>
    /// próprio (RL5-01, Refatoração Lote-5, correção do achado de validação do Lote 5).
    /// <para>
    /// Consumida por <see cref="Servicos.QuitacaoService"/>/<see cref="Servicos.CancelamentoService"/>
    /// (T-18/T-19), que chamam <see cref="Abrir"/> uma vez por tentativa dentro de
    /// <see cref="Servicos.ExecutorComRetry"/> (T-16): assim, se a 1ª tentativa falhar
    /// por conflito de concorrência (ex.: violação de <c>UNIQUE(VENDA_ID)</c> numa
    /// corrida de criação), a entidade <c>Added</c> "zumbi" fica presa ao
    /// <c>DbContext</c> descartado daquela tentativa — nunca vaza para a 2ª tentativa,
    /// que enxerga o banco com um <c>DbContext</c> limpo.
    /// </para>
    /// <para>
    /// Implementação real (EF6/Firebird) em Infrastructure (T-26, composition root
    /// Autofac): abre uma nova conexão/<c>FinanceiroDbContext</c> a cada chamada,
    /// dentro do mesmo escopo de operação/requisição já definido pela regra 5 da
    /// Seção 1/RT-08 (nunca compartilhado entre requisições diferentes — só entre
    /// tentativas da mesma operação).
    /// </para>
    /// </summary>
    public interface IFabricaEscopoOperacao
    {
        /// <summary>Abre um novo <see cref="IEscopoOperacao"/> (novo <c>DbContext</c>) para uma tentativa.</summary>
        IEscopoOperacao Abrir();
    }
}
