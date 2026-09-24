using System;
using System.Configuration;
using System.Reflection;
using Autofac;
using Autofac.Integration.WebApi;
using ERPFinanceiro.Api;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Application.Servicos;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Infrastructure;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Composition root Autofac do Desktop (T-26, SDD 2.1: única referência da
    /// solução a <c>ERPFinanceiro.Infrastructure</c> fora dela mesma). Registra todas
    /// as interfaces de Application -> implementações reais de Infrastructure/Api,
    /// com <c>InstancePerLifetimeScope</c> (escopo por requisição/operação) para
    /// <see cref="FinanceiroDbContext"/> e tudo que dependa dele diretamente — nunca
    /// <c>SingleInstance</c> para o contexto (TASK.md Seção 1, regra 5; RT-08).
    /// <para>
    /// Quem abre um <see cref="ILifetimeScope"/> por requisição/operação é o chamador
    /// (host OWIN, T-27; tela, T-42) — este composition root só monta o
    /// <see cref="IContainer"/> raiz. <see cref="QuitacaoService"/>/<see cref="CancelamentoService"/>
    /// não dependem do escopo Autofac diretamente: recebem <see cref="IFabricaEscopoOperacao"/>
    /// (RL5-01), que abre seu próprio <c>DbContext</c> a cada tentativa de retry — registrada
    /// aqui como <c>SingleInstance</c> porque ela mesma não guarda nenhum <c>DbContext</c>,
    /// só sabe criar um novo a cada <see cref="IFabricaEscopoOperacao.Abrir"/>.
    /// </para>
    /// <para>
    /// Registro de controllers da Api (<c>Autofac.WebApi2</c>/<c>RegisterApiControllers</c>)
    /// e o bind do container aqui montado ao host OWIN ficam com T-27 (ainda não
    /// implementada) — fora do escopo desta tarefa, que só cobre o composition root em si
    /// (critério de aceite de T-26: resolver <see cref="QuitacaoService"/> sem exceção).
    /// </para>
    /// </summary>
    public static class CompositionRoot
    {
        /// <summary>Monta e constrói o container Autofac raiz da aplicação Desktop.</summary>
        public static IContainer Construir()
        {
            var builder = new ContainerBuilder();
            RegistrarConfiguracaoEInfraestruturaComum(builder);
            RegistrarPersistenciaPorOperacao(builder);
            RegistrarServicosDeAplicacao(builder);
            RegistrarControllersApi(builder);
            return builder.Build();
        }

        /// <summary>
        /// Dependências sem estado ligado a uma operação/requisição específica:
        /// configuração (<see cref="IConfiguracaoBanco"/>), relógio (<see cref="IClock"/>),
        /// log (<see cref="IAppLogger"/>) e a verificação de saúde do banco
        /// (<see cref="IHealthService"/>, T-22 — abre sua própria conexão curta por
        /// chamada, não precisa de escopo por operação).
        /// </summary>
        private static void RegistrarConfiguracaoEInfraestruturaComum(ContainerBuilder builder)
        {
            builder.RegisterType<ConfiguracaoBancoAppConfig>()
                .As<IConfiguracaoBanco>()
                .SingleInstance();

            // IClock (T-26): implementação real, sempre UTC (regra 4 da Seção 1) — sem
            // estado, SingleInstance é seguro.
            builder.RegisterType<RelogioSistema>()
                .As<IClock>()
                .SingleInstance();

            // ICorrelationContext (T-26): implementação da Api (CallContext.Logical*,
            // ADR-004 — hospedagem OWIN "puro", sem System.Web). Escopo por
            // operação/requisição: cada requisição tem o seu próprio correlation id
            // (T-29 popula via CorrelationIdHandler quando a Api estiver hospedada, T-27).
            builder.RegisterType<CorrelationContext>()
                .As<ICorrelationContext>()
                .InstancePerLifetimeScope();

            // IAppLogger (T-26): TraceLogger (T-21), caminho do arquivo lido de
            // App.config (Log:CaminhoArquivo, ver App.config.example) — nunca hardcoded.
            // SingleInstance: um único arquivo/listener de log para todo o processo.
            builder.Register(_ => new TraceLogger(ObterConfiguracaoObrigatoria("Log:CaminhoArquivo")))
                .As<IAppLogger>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();
        }

        /// <summary>
        /// <see cref="FinanceiroDbContext"/> e tudo que é montado diretamente por cima
        /// dele (<see cref="IVendaRepository"/>/<see cref="IVendaConsultaLeitura"/>/
        /// <see cref="IUnitOfWork"/>): <c>InstancePerLifetimeScope</c> — um contexto novo
        /// por escopo Autofac (requisição/operação), nunca <c>SingleInstance</c> (regra 5
        /// da Seção 1/RT-08). <see cref="IFabricaEscopoOperacao"/> (RL5-01) é a exceção
        /// deliberada: não half-share nenhum <c>DbContext</c> — cada chamada a
        /// <see cref="IFabricaEscopoOperacao.Abrir"/> cria o seu, por isso pode ser
        /// <c>SingleInstance</c> com segurança.
        /// </summary>
        private static void RegistrarPersistenciaPorOperacao(ContainerBuilder builder)
        {
            builder.Register(ctx =>
                {
                    var configuracao = ctx.Resolve<IConfiguracaoBanco>();
                    var connection = new FbConnection(MontarConnectionString(configuracao));
                    return new FinanceiroDbContext(connection);
                })
                .InstancePerLifetimeScope();

            builder.RegisterType<VendaRepository>()
                .As<IVendaRepository>()
                .As<IVendaConsultaLeitura>()
                .InstancePerLifetimeScope();

            builder.RegisterType<UnitOfWork>()
                .As<IUnitOfWork>()
                .InstancePerLifetimeScope();

            // RL5-01: fábrica de escopo por tentativa de retry, consumida por
            // QuitacaoService/CancelamentoService — não reaproveita o DbContext acima
            // (InstancePerLifetimeScope), abre um novo a cada tentativa (ver
            // FabricaEscopoOperacaoEf).
            builder.RegisterType<FabricaEscopoOperacaoEf>()
                .As<IFabricaEscopoOperacao>()
                .SingleInstance();
        }

        /// <summary>
        /// Serviços de aplicação (T-18/T-19/T-24 consultas). <c>InstancePerLifetimeScope</c>
        /// para acompanhar o ciclo de vida do escopo por operação/requisição — mesmo que
        /// <see cref="QuitacaoService"/>/<see cref="CancelamentoService"/> não usem mais
        /// diretamente o <see cref="FinanceiroDbContext"/> do escopo (RL5-01, usam
        /// <see cref="IFabricaEscopoOperacao"/>), <see cref="ConsultaService"/> continua
        /// lendo do par <see cref="IVendaRepository"/>/<see cref="IVendaConsultaLeitura"/>
        /// do escopo atual.
        /// </summary>
        private static void RegistrarServicosDeAplicacao(ContainerBuilder builder)
        {
            builder.RegisterType<QuitacaoService>().InstancePerLifetimeScope();
            builder.RegisterType<CancelamentoService>().InstancePerLifetimeScope();
            builder.RegisterType<RegistroService>().InstancePerLifetimeScope();
            builder.RegisterType<ConsultaService>().InstancePerLifetimeScope();
        }

        /// <summary>
        /// Registra os controllers Web API da Api (T-30: <c>VendasController</c> e as
        /// actions que T-31/T-32 adicionarem a ele) no container Autofac, para que
        /// <see cref="Api.Startup"/>/<c>AutofacWebApiDependencyResolver</c> (T-27) resolva
        /// a injeção por construtor de cada controller (ex.: <c>QuitacaoService</c> em
        /// <c>VendasController</c>) por requisição, em vez de exigir construtor sem
        /// parâmetro. Concretiza a nota de T-26/T-27 que deixava este passo pendente.
        /// </summary>
        private static void RegistrarControllersApi(ContainerBuilder builder)
        {
            builder.RegisterApiControllers(typeof(Api.Startup).GetTypeInfo().Assembly);
        }

        private static string MontarConnectionString(IConfiguracaoBanco configuracao)
        {
            var csb = new FbConnectionStringBuilder
            {
                Database = configuracao.CaminhoFdb,
                UserID = configuracao.Usuario,
                Password = configuracao.Senha,
                ServerType = FbServerType.Embedded,
                Charset = "UTF8",
                Pooling = false
            };
            return csb.ConnectionString;
        }

        private static string ObterConfiguracaoObrigatoria(string chave)
        {
            string valor = ConfigurationManager.AppSettings[chave];
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new InvalidOperationException(
                    $"Configuração obrigatória ausente: appSettings/{chave} (App.config). " +
                    "Veja App.config.example.");
            }

            return valor;
        }
    }
}
