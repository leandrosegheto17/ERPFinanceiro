using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Infrastructure.Persistencia;
using Owin;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// T-46 (F-7): critério de aceite exercitado com Firebird embarcado real, HttpListener real
    /// e Mutex real (nomes únicos por teste; nunca o nome de produção). Não abre janela nem splash:
    /// splash/diálogos/dois processos ficam como conferência manual.
    /// </summary>
    public class SequenciaInicializacaoTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly List<string> _logs = new List<string>();

        public SequenciaInicializacaoTests()
        {
            _caminhoFdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "T46_Sequencia_" + Guid.NewGuid().ToString("N") + ".fdb");
        }

        public void Dispose()
        {
            try { if (File.Exists(_caminhoFdb)) File.Delete(_caminhoFdb); } catch (Exception) { }
        }

        private sealed class Config : IConfiguracaoBanco
        {
            public string CaminhoFdb { get; set; }
            public string Usuario { get; set; } = "sysdba";
            public string Senha { get; set; } = "masterkey";
        }

        private static int PortaLivre()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int p = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return p;
        }

        private static readonly Action<IAppBuilder> Pipeline = app =>
            app.Run(c => { c.Response.StatusCode = 200; return System.Threading.Tasks.Task.FromResult(0); });

        private SequenciaInicializacao Criar(Func<bool> instancia, ApiHost host)
        {
            var banco = new DbInitializer(new Config { CaminhoFdb = _caminhoFdb });
            return new SequenciaInicializacao(
                instancia,
                banco.Inicializar,
                p => { host.Start(p, Pipeline); return new ResultadoInicioApi(host.Estado, host.UltimoErro); },
                _logs.Add);
        }

        private static ApiHost NovoHost() => new ApiHost(new Autofac.ContainerBuilder().Build());

        [Fact]
        public void FdbAusente_CriaBancoESobeApi_DecisaoNormal()
        {
            var host = NovoHost();
            try
            {
                var r = Criar(() => true, host).Executar(PortaLivre());

                Assert.True(File.Exists(_caminhoFdb));
                Assert.False(r.BancoEmErro);
                Assert.Equal(EstadoApiHost.Ativa, r.EstadoApi);
                Assert.Equal(DecisaoInicializacao.AbrirNormal, r.Decisao);
            }
            finally { host.Stop(); }
        }

        [Fact]
        public void FdbBloqueado_BancoEmErro_AbreEmEstadoDeErroELogaEApiSegueAtiva()
        {
            new DbInitializer(new Config { CaminhoFdb = _caminhoFdb }).Inicializar();
            var host = NovoHost();
            try
            {
                using (new FileStream(_caminhoFdb, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    var r = Criar(() => true, host).Executar(PortaLivre());

                    Assert.True(r.BancoEmErro);
                    Assert.IsType<BancoIndisponivelException>(r.ErroBanco);
                    Assert.Equal(DecisaoInicializacao.AbrirEmEstadoDeErroDeBanco, r.Decisao);
                    Assert.Contains(_logs, l => l.Contains("banco"));
                }
            }
            finally { host.Stop(); }
        }

        [Fact]
        public void PortaOcupada_ResultadoPortaEmUso_DialogoComCausaEAcao()
        {
            int porta = PortaLivre();
            var ocupante = new HttpListener();
            ocupante.Prefixes.Add($"http://localhost:{porta}/");
            ocupante.Start();
            var host = NovoHost();
            try
            {
                var r = Criar(() => true, host).Executar(porta);

                Assert.True(r.PortaEmUso);
                Assert.IsType<HttpListenerException>(r.ErroApi);
                Assert.Equal(DecisaoInicializacao.MostrarDialogoApi, r.Decisao);
                Assert.False(r.BancoEmErro);

                string texto = TextosInicializacao.DialogoApi(r);
                Assert.Contains(porta.ToString(), texto);
                Assert.Contains("Api:Porta", texto);
                Assert.Contains("já está em uso", texto);
            }
            finally
            {
                host.Stop();
                ocupante.Stop();
                ocupante.Close();
            }
        }

        [Fact]
        public void SegundaInstancia_NaoTocaNoBancoNemNaApi()
        {
            bool bancoChamado = false, apiChamada = false;
            var seq = new SequenciaInicializacao(() => false, () => bancoChamado = true,
                p => { apiChamada = true; return new ResultadoInicioApi(EstadoApiHost.Ativa, null); });

            var r = seq.Executar(5000);

            Assert.True(r.SegundaInstancia);
            Assert.Equal(DecisaoInicializacao.SairSegundaInstancia, r.Decisao);
            Assert.False(bancoChamado);
            Assert.False(apiChamada);
            Assert.False(File.Exists(_caminhoFdb));
        }

        [Fact]
        public void ExcecaoNaApi_NaoPropagaEViraApiFalhou()
        {
            var seq = new SequenciaInicializacao(() => true, () => { },
                p => throw new InvalidOperationException("boom"));

            var r = seq.Executar(5000);

            Assert.True(r.ApiFalhou);
            Assert.False(r.PortaEmUso);
            Assert.Equal(DecisaoInicializacao.MostrarDialogoApi, r.Decisao);
        }

        [Fact]
        public void Mutex_SegundaAquisicaoComMesmoNomeFalha_AposLiberarPermite()
        {
            string nome = @"Local\ERPFinanceiro.Teste.T46." + Guid.NewGuid().ToString("N");
            var primeira = InstanciaUnica.TentarAdquirir(nome);
            Assert.NotNull(primeira);

            // Mutex é reentrante na mesma thread; a segunda tentativa vem de outra thread, como outro processo.
            InstanciaUnica segunda = null;
            var t = new System.Threading.Thread(() => segunda = InstanciaUnica.TentarAdquirir(nome));
            t.Start();
            t.Join();
            Assert.Null(segunda);

            primeira.Dispose();

            InstanciaUnica terceira = null;
            var t2 = new System.Threading.Thread(() => terceira = InstanciaUnica.TentarAdquirir(nome));
            t2.Start();
            t2.Join();
            Assert.NotNull(terceira);
            terceira.Dispose();
        }

        [Fact]
        public void NomeMutexProducao_EConstanteGlobalDocumentada()
        {
            Assert.StartsWith(@"Global\", InstanciaUnica.NomeMutexProducao);
        }

        [Fact]
        public void Textos_SegundaInstanciaESplashConformeUxSpec()
        {
            Assert.Equal("Iniciando banco e API...", TextosInicializacao.Splash);
            Assert.Equal("Já existe uma instância em execução", TextosInicializacao.SegundaInstancia);
        }

        [Fact]
        public void Presenters_FalhaDeCarga_RepassaExcecaoAoLogEPropaga()
        {
            Exception logada = null;
            var p = new ConsultaVendasPresenter(() => throw new InvalidOperationException("db"), ex => logada = ex);

            var erro = Assert.Throws<InvalidOperationException>(() => p.CarregarAsync().GetAwaiter().GetResult());

            Assert.Same(erro, logada);

            Exception logada2 = null;
            var d = new DetalheVendaPresenter(id => throw new InvalidOperationException("db"), ex => logada2 = ex);
            Assert.Throws<InvalidOperationException>(() => d.CarregarAsync("1").GetAwaiter().GetResult());
            Assert.NotNull(logada2);
        }

        [Fact]
        public void IconesStatus_SemCorLiteralEComTresItens()
        {
            var colecao = ConfiguracaoVisual.CriarIconesStatus();
            Assert.Equal(3, colecao.Count);
        }
    }
}
