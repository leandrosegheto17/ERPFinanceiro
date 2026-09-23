using System;
using System.Net;
using Autofac;
using ERPFinanceiro.Api;
using Microsoft.Owin.Hosting;
using Owin;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Estado consultável do host OWIN (T-27). A barra de status (T-45) e a
    /// inicialização (T-46) leem <see cref="ApiHost.Estado"/> em vez de deixar uma
    /// eventual falha de bind estourar e derrubar o app (ADR-004: "falha de bind
    /// precisa de tratamento visível").
    /// </summary>
    public enum EstadoApiHost
    {
        /// <summary>Ainda não iniciado, ou já parado por <see cref="ApiHost.Stop"/>.</summary>
        Inativa,

        /// <summary>Bind em andamento, dentro de <see cref="ApiHost.Start(int)"/>.</summary>
        Iniciando,

        /// <summary>Bind concluído; host servindo requisições em <see cref="ApiHost.EnderecoBase"/>.</summary>
        Ativa,

        /// <summary>
        /// Bind falhou porque a porta já está em uso por outro processo/listener
        /// (<see cref="HttpListenerException"/>). O app continua rodando; ver
        /// <see cref="ApiHost.UltimoErro"/> para a exceção original.
        /// </summary>
        InativaPortaEmUso
    }

    /// <summary>
    /// Host OWIN self-host do Desktop (T-27, ADR-004): <see cref="Start"/>/<see cref="Stop"/>
    /// isolam o self-host (<c>Microsoft.Owin.Host.HttpListener</c> via
    /// <c>Microsoft.Owin.Hosting.WebApp</c>) em <c>http://localhost:{porta}/</c>, para poder
    /// migrar a serviço Windows/IIS depois "sem mexer nas regras" (texto do ADR-004) — a
    /// composição OWIN de verdade continua inteira em <see cref="Startup"/>.
    /// <para>
    /// Conecta o composition root Autofac (<see cref="CompositionRoot.Construir"/>, T-26) ao
    /// container que <see cref="Startup"/>/<c>Autofac.WebApi2</c> usa: o construtor recebe o
    /// <see cref="IContainer"/> raiz e <see cref="Start(int)"/> o atribui a
    /// <see cref="Startup.Container"/> antes de <c>WebApp.Start&lt;Startup&gt;(url)</c> (ver
    /// XML doc de <see cref="Startup.Container"/> sobre por que a atribuição é estática).
    /// </para>
    /// <para>
    /// Falha de bind (porta ocupada, <see cref="HttpListenerException"/>) é capturada aqui:
    /// <see cref="Estado"/> vira <see cref="EstadoApiHost.InativaPortaEmUso"/> e
    /// <see cref="UltimoErro"/> guarda a exceção original — nunca deixa a exceção subir e
    /// derrubar o app (critério de aceite de T-27).
    /// </para>
    /// </summary>
    public sealed class ApiHost : IDisposable
    {
        private readonly IContainer _container;
        private readonly object _sync = new object();
        private IDisposable _webApp;

        public ApiHost(IContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>Estado atual do host — nunca lança, sempre consultável (T-27/T-45).</summary>
        public EstadoApiHost Estado { get; private set; } = EstadoApiHost.Inativa;

        /// <summary>
        /// Última exceção de bind capturada (ex.: <see cref="HttpListenerException"/> por
        /// porta em uso). <c>null</c> se nunca houve falha. Exposta para o diálogo de
        /// status (T-45: "porta, caminho .fdb, último erro, Copiar").
        /// </summary>
        public Exception UltimoErro { get; private set; }

        /// <summary>Endereço base servido enquanto <see cref="Estado"/> é <see cref="EstadoApiHost.Ativa"/>.</summary>
        public string EnderecoBase { get; private set; }

        /// <summary>
        /// Sobe o host em <c>http://localhost:{porta}/</c> usando a composição OWIN real da
        /// Api (<see cref="Startup"/>) conectada ao container Autofac deste
        /// <see cref="ApiHost"/> — caminho de produção (T-46 chama esta sobrecarga).
        /// </summary>
        public void Start(int porta)
        {
            Startup.Container = _container;
            Start(porta, () => WebApp.Start<Startup>($"http://localhost:{porta}/"));
        }

        /// <summary>
        /// Sobrecarga usada só por teste automatizado (<c>ApiHostTests</c>, T-27): injeta um
        /// pipeline OWIN mínimo próprio para exercitar bind/estado/porta-em-uso sem depender
        /// de nenhum controller Web API real (nenhum existe ainda — T-30/T-31/T-32 são
        /// tarefas futuras). O caminho de produção continua sendo <see cref="Start(int)"/>,
        /// que sempre usa <see cref="Startup"/> com o container real.
        /// </summary>
        internal void Start(int porta, Action<IAppBuilder> configurarOwinDeTeste)
        {
            Start(porta, () => WebApp.Start(
                new StartOptions($"http://localhost:{porta}/"),
                configurarOwinDeTeste));
        }

        private void Start(int porta, Func<IDisposable> iniciar)
        {
            lock (_sync)
            {
                Estado = EstadoApiHost.Iniciando;
                UltimoErro = null;

                try
                {
                    _webApp = iniciar();
                    EnderecoBase = $"http://localhost:{porta}/";
                    Estado = EstadoApiHost.Ativa;
                }
                catch (Exception ex) when (ExtrairHttpListenerException(ex) != null)
                {
                    UltimoErro = ExtrairHttpListenerException(ex);
                    Estado = EstadoApiHost.InativaPortaEmUso;
                    EnderecoBase = null;
                }
            }
        }

        /// <summary>
        /// Libera a porta (confirmável reabrindo-a em seguida — critério de aceite de T-27).
        /// Idempotente: chamar sem um <see cref="Start(int)"/> anterior (ou repetidamente) não
        /// lança.
        /// </summary>
        public void Stop()
        {
            lock (_sync)
            {
                _webApp?.Dispose();
                _webApp = null;
                EnderecoBase = null;

                if (Estado != EstadoApiHost.InativaPortaEmUso)
                {
                    Estado = EstadoApiHost.Inativa;
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// <c>WebApp.Start</c> pode propagar o <see cref="HttpListenerException"/> de bind
        /// diretamente ou embrulhado (reflexão do carregador de startup do Katana) — percorre
        /// a cadeia de <see cref="Exception.InnerException"/> para achar o original em
        /// qualquer um dos dois casos.
        /// </summary>
        private static HttpListenerException ExtrairHttpListenerException(Exception ex)
        {
            while (ex != null)
            {
                if (ex is HttpListenerException listenerException)
                {
                    return listenerException;
                }

                ex = ex.InnerException;
            }

            return null;
        }
    }
}
