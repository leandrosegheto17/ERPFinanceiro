using System.Text;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Autofac;
using Autofac.Integration.WebApi;
using ERPFinanceiro.Api.Erros;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;

namespace ERPFinanceiro.Api
{
    /// <summary>
    /// Composição OWIN da Api (T-25): <see cref="HttpConfiguration"/> com attribute
    /// routing e o formatter JSON exigido pelo contrato v1.1 (Seção 2) — UTF-8,
    /// propriedades em camelCase, decimal serializado como número (não string) e
    /// datas em ISO 8601 UTC terminando em <c>Z</c> (P-6).
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Container Autofac raiz usado por <see cref="Configuration"/> (T-27):
        /// <c>ApiHost.Start</c> atribui aqui o container real montado pelo
        /// composition root do Desktop (<see cref="ERPFinanceiro.Desktop.CompositionRoot.Construir"/>,
        /// T-26) antes de chamar <c>WebApp.Start&lt;Startup&gt;(url)</c>. Estático
        /// porque o host OWIN (<c>Microsoft.Owin.Hosting</c>) instancia <see cref="Startup"/>
        /// por reflexão via <c>Activator.CreateInstance</c> (construtor sem parâmetro
        /// exigido pela assinatura genérica <c>WebApp.Start&lt;Startup&gt;</c>) — não há
        /// como injetar o container por construtor nesse caminho. Seguro neste app porque
        /// só existe um processo/uma instância de host por vez (ADR-004: "um executável,
        /// um processo dono do <c>.fdb</c>"). Se <c>null</c> (ex.: <see cref="StartupTests"/>
        /// exercitando só <see cref="CriarConfiguracaoJson"/>, sem subir host), um container
        /// Autofac vazio é usado (nenhum controller resolve dependência externa até T-30+).
        /// </summary>
        public static Autofac.IContainer Container { get; set; }

        /// <summary>
        /// Ponto de entrada OWIN (assinatura exigida pelo host, ex.
        /// <c>WebApp.Start&lt;Startup&gt;(url)</c> em <c>ApiHost</c>, T-27).
        /// </summary>
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            ConfigurarRoteamento(config);
            ConfigurarSerializacaoJson(config);
            ConfigurarAutofac(config);
            ConfigurarCorrelationId(config);
            ConfigurarApiKey(config);
            ConfigurarExceptionHandler(config);

            app.UseWebApi(config);
        }

        /// <summary>
        /// Registra o <see cref="GlobalExceptionHandler"/> (T-28) como
        /// <see cref="IExceptionHandler"/> global — qualquer exceção não tratada pelo
        /// controller vira o envelope <c>{erro:{codigo,mensagem}}</c> do contrato v1.1
        /// (Seção 2), nunca stack trace (TASK.md Seção 1, regra 7).
        /// </summary>
        private static void ConfigurarExceptionHandler(HttpConfiguration config)
        {
            config.Services.Replace(typeof(IExceptionHandler), new GlobalExceptionHandler());
        }

        /// <summary>
        /// Registra o <see cref="CorrelationIdHandler"/> (T-29) como message handler
        /// global do pipeline — roda para toda requisição, antes do roteamento de
        /// controller, garantindo que toda resposta traga <c>X-Correlation-Id</c>.
        /// </summary>
        private static void ConfigurarCorrelationId(HttpConfiguration config)
        {
            config.MessageHandlers.Add(new CorrelationIdHandler(new CorrelationContext()));
        }

        /// <summary>
        /// Registra o <see cref="ApiKeyHandler"/> (T-35) como message handler global —
        /// roda antes do roteamento de controller (mesmo mecanismo de
        /// <see cref="CorrelationIdHandler"/> acima, adicionado depois dele para que o
        /// <c>X-Correlation-Id</c> já esteja populado quando o log de tentativa não
        /// autorizada for gravado), garantindo que nenhuma rota autenticável escape da
        /// checagem de <c>X-Api-Key</c> (contrato v1.1 Seção 2, isenta só
        /// <c>GET /api/health</c>, T-36).
        /// </summary>
        private static void ConfigurarApiKey(HttpConfiguration config)
        {
            config.MessageHandlers.Add(new ApiKeyHandler());
        }

        private static void ConfigurarRoteamento(HttpConfiguration config)
        {
            // Attribute routing: cada controller/action declara sua própria rota
            // (ex. [RoutePrefix("api/vendas")]/[Route("quitacao")], T-30/T-31/T-32).
            config.MapHttpAttributeRoutes();
        }

        private static void ConfigurarSerializacaoJson(HttpConfiguration config)
        {
            // JSON como único formato de saída relevante para o contrato v1.1.
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            var jsonFormatter = config.Formatters.JsonFormatter;
            jsonFormatter.SupportedEncodings.Clear();
            jsonFormatter.SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            jsonFormatter.SerializerSettings = CriarConfiguracaoJson();
        }

        /// <summary>
        /// Mesma configuração de serialização usada pelo formatter JSON da Api,
        /// exposta à parte para ser exercitada por teste automatizado sem precisar
        /// subir o host OWIN (critério de aceite de T-25: comprova decimal como
        /// número e data terminando em "Z" via <see cref="JsonConvert"/> direto).
        /// </summary>
        public static JsonSerializerSettings CriarConfiguracaoJson()
        {
            var settings = new JsonSerializerSettings
            {
                // camelCase nas propriedades (contrato v1.1 Seção 2).
                ContractResolver = new CamelCasePropertyNamesContractResolver(),

                // Datas em ISO 8601 UTC terminando em "Z": converte qualquer DateTime
                // não-UTC para UTC antes de serializar; o formato ISO padrão do
                // Json.NET (DateFormatHandling.IsoDateFormat, já o default) grava "Z"
                // para Kind=Utc.
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };

            // Decimal como número JSON, não string (P-6): comportamento padrão do
            // Json.NET para System.Decimal — nenhum converter adicional necessário;
            // documentado aqui para não ser "corrigido" por engano depois.
            return settings;
        }

        private static void ConfigurarAutofac(HttpConfiguration config)
        {
            // T-27: usa o container real do composition root do Desktop (T-26), passado
            // via Startup.Container por ApiHost.Start antes de WebApp.Start<Startup>(url).
            // Fallback para um container vazio só quando ninguém o atribuiu (StartupTests
            // exercitando CriarConfiguracaoJson diretamente, sem host OWIN de verdade) —
            // nenhum controller registrado ainda depende de algo externo (T-30+).
            IContainer container = Container ?? new ContainerBuilder().Build();

            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}
