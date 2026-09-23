using Autofac;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Application.Servicos;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Desktop;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// T-26 (Lote 6): critério de aceite literal — "app sobe e resolve
    /// <see cref="QuitacaoService"/> sem exceção" — via <c>builder.Build()</c> +
    /// <c>scope.Resolve&lt;QuitacaoService&gt;()</c>, contra o composition root real
    /// (<see cref="CompositionRoot"/>), não um container de teste à parte. Não abre
    /// nenhum <c>.fdb</c>/arquivo de log de verdade: todo o grafo necessário para
    /// resolver os serviços de aplicação só constrói connection strings/caminhos
    /// (adiado até o primeiro uso real, ex. <see cref="IFabricaEscopoOperacao.Abrir"/>).
    /// </summary>
    public class CompositionRootTests
    {
        [Fact]
        public void Construir_ResolveQuitacaoServiceSemExcecao()
        {
            using (var container = CompositionRoot.Construir())
            using (var escopo = container.BeginLifetimeScope())
            {
                var servico = escopo.Resolve<QuitacaoService>();

                Assert.NotNull(servico);
            }
        }

        [Fact]
        public void Construir_ResolveCancelamentoServiceEConsultaServiceSemExcecao()
        {
            using (var container = CompositionRoot.Construir())
            using (var escopo = container.BeginLifetimeScope())
            {
                Assert.NotNull(escopo.Resolve<CancelamentoService>());
                Assert.NotNull(escopo.Resolve<ConsultaService>());
                Assert.NotNull(escopo.Resolve<IHealthService>());
            }
        }

        /// <summary>
        /// Regra 5 da Seção 1/RT-08: <c>DbContext</c> por operação/requisição, nunca
        /// compartilhado. Confirma o Autofac <c>InstancePerLifetimeScope</c> registrado
        /// em <see cref="CompositionRoot"/>: dentro do MESMO escopo, o mesmo
        /// <see cref="IVendaRepository"/> é devolvido (mesmo <c>DbContext</c> por trás,
        /// consistente com <see cref="IUnitOfWork"/> da mesma operação); em um escopo
        /// NOVO, a instância é diferente (novo <c>DbContext</c>, nova operação).
        /// </summary>
        [Fact]
        public void Construir_IVendaRepository_MesmaInstanciaDentroDoEscopoDiferenteEntreEscopos()
        {
            using (var container = CompositionRoot.Construir())
            {
                using (var escopo1 = container.BeginLifetimeScope())
                {
                    var repositorioA = escopo1.Resolve<IVendaRepository>();
                    var repositorioB = escopo1.Resolve<IVendaRepository>();

                    Assert.Same(repositorioA, repositorioB);
                }

                IVendaRepository repositorioDoEscopo1;
                using (var escopo1 = container.BeginLifetimeScope())
                {
                    repositorioDoEscopo1 = escopo1.Resolve<IVendaRepository>();
                }

                using (var escopo2 = container.BeginLifetimeScope())
                {
                    var repositorioDoEscopo2 = escopo2.Resolve<IVendaRepository>();

                    Assert.NotSame(repositorioDoEscopo1, repositorioDoEscopo2);
                }
            }
        }

        /// <summary>
        /// RL5-01: <see cref="IFabricaEscopoOperacao"/> nunca é o mesmo <c>DbContext</c>
        /// do escopo por requisição — cada <see cref="IFabricaEscopoOperacao.Abrir"/>
        /// abre um <see cref="IEscopoOperacao"/> (e portanto um <c>DbContext</c>) novo,
        /// mesmo dentro do mesmo escopo Autofac. Confirma só a forma (sem abrir conexão
        /// real): duas chamadas a <c>Abrir()</c> devolvem instâncias de
        /// <see cref="IVendaRepository"/> diferentes.
        /// </summary>
        [Fact]
        public void Construir_IFabricaEscopoOperacao_AbrirDevolveEscopoNovoACadaChamada()
        {
            using (var container = CompositionRoot.Construir())
            using (var escopo = container.BeginLifetimeScope())
            {
                var fabrica = escopo.Resolve<IFabricaEscopoOperacao>();

                using (var escopoOperacaoA = fabrica.Abrir())
                using (var escopoOperacaoB = fabrica.Abrir())
                {
                    Assert.NotSame(escopoOperacaoA.Repositorio, escopoOperacaoB.Repositorio);
                    Assert.NotSame(escopoOperacaoA.UnitOfWork, escopoOperacaoB.UnitOfWork);
                }
            }
        }
    }
}
