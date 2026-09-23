using System;

namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Escopo de uma única tentativa de operação de negócio (RL5-01, Refatoração
    /// Lote-5): expõe um par <see cref="Repositorio"/>/<see cref="UnitOfWork"/> que
    /// compartilham o mesmo <c>DbContext</c> por trás, criado especificamente para
    /// esta tentativa (ver <see cref="IFabricaEscopoOperacao"/>). <see cref="Dispose"/>
    /// libera a conexão/contexto ao final da tentativa — o próximo escopo, se houver
    /// (retry de <see cref="Servicos.ExecutorComRetry"/>, T-16), nunca reaproveita
    /// este <c>DbContext</c>.
    /// </summary>
    public interface IEscopoOperacao : IDisposable
    {
        /// <summary>Repositório de vendas desta tentativa (mesmo <c>DbContext</c> de <see cref="UnitOfWork"/>).</summary>
        IVendaRepository Repositorio { get; }

        /// <summary>Fronteira transacional desta tentativa (mesmo <c>DbContext</c> de <see cref="Repositorio"/>).</summary>
        IUnitOfWork UnitOfWork { get; }
    }
}
