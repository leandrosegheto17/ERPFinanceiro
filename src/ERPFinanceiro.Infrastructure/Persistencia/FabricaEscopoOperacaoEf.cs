using System;
using ERPFinanceiro.Application.Interfaces;
using FirebirdSql.Data.FirebirdClient;

namespace ERPFinanceiro.Infrastructure.Persistencia
{
    /// <summary>
    /// Implementação EF6/Firebird de <see cref="IFabricaEscopoOperacao"/> (RL5-01,
    /// resolvida em T-26 — composition root). Cada chamada a <see cref="Abrir"/> abre
    /// uma <see cref="FbConnection"/> e um <see cref="FinanceiroDbContext"/> novos —
    /// nunca reaproveita a conexão/contexto de uma tentativa anterior — e devolve um
    /// <see cref="IEscopoOperacao"/> cujo <see cref="IEscopoOperacao.Dispose"/> fecha
    /// essa conexão (mesmo padrão de conexão "curta, própria" já usado por
    /// <see cref="HealthService"/>, T-22).
    /// </summary>
    public sealed class FabricaEscopoOperacaoEf : IFabricaEscopoOperacao
    {
        private readonly IConfiguracaoBanco _configuracao;

        public FabricaEscopoOperacaoEf(IConfiguracaoBanco configuracao)
        {
            _configuracao = configuracao ?? throw new ArgumentNullException(nameof(configuracao));
        }

        /// <inheritdoc />
        public IEscopoOperacao Abrir()
        {
            var connection = new FbConnection(MontarConnectionString());
            var contexto = new FinanceiroDbContext(connection); // contextOwnsConnection = true (default)

            return new EscopoOperacaoEf(contexto);
        }

        private string MontarConnectionString()
        {
            var csb = new FbConnectionStringBuilder
            {
                Database = _configuracao.CaminhoFdb,
                UserID = _configuracao.Usuario,
                Password = _configuracao.Senha,
                ServerType = FbServerType.Embedded,
                Charset = "UTF8",
                Pooling = false
            };
            return csb.ConnectionString;
        }

        /// <summary>
        /// <see cref="IEscopoOperacao"/> concreto: um <see cref="FinanceiroDbContext"/>
        /// só seu, com um <see cref="VendaRepository"/>/<see cref="UnitOfWork"/> por
        /// cima — <see cref="Dispose"/> descarta o contexto (e a conexão, já que
        /// <c>contextOwnsConnection</c> é <c>true</c>).
        /// </summary>
        private sealed class EscopoOperacaoEf : IEscopoOperacao
        {
            private readonly FinanceiroDbContext _contexto;
            private bool _disposto;

            public EscopoOperacaoEf(FinanceiroDbContext contexto)
            {
                _contexto = contexto;
                var repositorio = new VendaRepository(contexto);
                Repositorio = repositorio;
                UnitOfWork = new UnitOfWork(contexto);
            }

            public IVendaRepository Repositorio { get; }

            public IUnitOfWork UnitOfWork { get; }

            public void Dispose()
            {
                if (_disposto)
                {
                    return;
                }

                _disposto = true;
                _contexto.Dispose();
            }
        }
    }
}
