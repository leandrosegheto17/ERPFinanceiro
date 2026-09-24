using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using Autofac;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Infrastructure.Persistencia;
using Owin;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// T-45 (ligação) + T-47: montagem da janela, confirmação ao fechar e encerramento ordenado.
    /// ApiHost real em porta livre + Firebird embarcado real; sem Show()/Application.Run.
    /// O diálogo real, o fechamento real e o término do processo ficam para conferência manual.
    /// </summary>
    public class EncerramentoTests : IDisposable
    {
        private readonly string _caminhoFdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "T47_Encerramento_" + Guid.NewGuid().ToString("N") + ".fdb");

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

        private sealed class TimerFake : ITemporizador
        {
            public event EventHandler Tick;
            public TimeSpan? Intervalo;
            public bool Parado, Descartado;
            public void Iniciar(TimeSpan i) { Intervalo = i; }
            public void Parar() { Parado = true; }
            public void Dispose() { Descartado = true; }
        }

        private sealed class AreaFake : IAreaTransferencia { public void Copiar(string t) { } }

        private static readonly Action<IAppBuilder> Pipeline = app =>
            app.Run(c => { c.Response.StatusCode = 200; return System.Threading.Tasks.Task.FromResult(0); });

        private static int PortaLivre()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int p = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return p;
        }

        private static void EmSta(Action acao)
        {
            Exception erro = null;
            var th = new Thread(() => { try { acao(); } catch (Exception ex) { erro = ex; } });
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
            th.Join();
            if (erro != null) throw new Exception("Falha na thread STA: " + erro, erro);
        }

        // Iniciar() já dispara uma checagem em segundo plano (chamadas concorrentes são ignoradas): aguarda o resultado.
        private static void AguardarBancoOk(BarraStatusPresenter barra)
        {
            var limite = DateTime.UtcNow.AddSeconds(15);
            while (barra.Banco.Texto != "Banco: OK" && DateTime.UtcNow < limite) { System.Windows.Forms.Application.DoEvents(); Thread.Sleep(50); } // a continuação volta pela UI thread: precisa bombear mensagens
        }

        [Fact]
        public void Encerrar_executa_ordem_correta_e_e_idempotente()
        {
            var ordem = new List<string>();
            var e = new EncerramentoAplicacao(() => ordem.Add("barra"), () => ordem.Add("api"),
                () => ordem.Add("container"), () => ordem.Add("instancia"));
            e.Encerrar();
            e.Encerrar();
            Assert.Equal(new[] { "barra", "api", "container", "instancia" }, ordem);
            Assert.True(e.Encerrado);
        }

        [Fact]
        public void Falha_num_passo_nao_impede_os_seguintes()
        {
            var ordem = new List<string>();
            var falhas = new List<Exception>();
            var e = new EncerramentoAplicacao(() => throw new InvalidOperationException("x"),
                () => ordem.Add("api"), () => ordem.Add("container"), null, falhas.Add);
            e.Encerrar();
            Assert.Equal(new[] { "api", "container" }, ordem);
            Assert.Single(falhas);
        }

        [Fact]
        public void PodeFechar_respeita_a_resposta_e_texto_e_o_do_UX_SPEC()
        {
            var e = new EncerramentoAplicacao(null, null, null);
            Assert.False(e.PodeFechar(() => false));
            Assert.True(e.PodeFechar(() => true));
            Assert.Equal("Fechar encerra a API; o ERP Vendas não conseguirá enviar vendas.", EncerramentoAplicacao.TextoConfirmacao);
        }

        private IContainer CriarContainerReal()
        {
            var b = new ContainerBuilder();
            var cfg = new Config { CaminhoFdb = _caminhoFdb };
            b.RegisterInstance<IConfiguracaoBanco>(cfg);
            b.Register(c => new HealthService(cfg)).As<IHealthService>();
            return b.Build();
        }

        [Fact]
        public void Montagem_anexa_barra_e_inicia_timer_com_textos_esperados()
        {
            EmSta(() =>
            {
                var container = CriarContainerReal();
                new DbInitializer(new Config { CaminhoFdb = _caminhoFdb }).Inicializar();
                var host = new ApiHost(container);
                int porta = PortaLivre();
                var timer = new TimerFake();
                try
                {
                    host.Start(porta, Pipeline);
                    using (var form = new FrmConsulta(new ConsultaVendasPresenter(() => throw new InvalidOperationException())))
                    {
                        var comp = new ComposicaoJanelaPrincipal(form, container, host, porta, _caminhoFdb, timer,
                            new AreaFake(), () => true);
                        Assert.NotNull(form.BarraStatus);
                        Assert.Equal(BarraStatusPresenter.IntervaloPadrao, timer.Intervalo);
                        AguardarBancoOk(comp.Barra);
                        Assert.Equal("API: Ativa (http://localhost:" + porta + ")", comp.Barra.Api.Texto);
                        Assert.Equal("Banco: OK", comp.Barra.Banco.Texto);
                        Assert.Equal(_caminhoFdb, comp.Barra.CaminhoFdb);
                    }
                }
                finally { host.Stop(); container.Dispose(); }
            });
        }

        [Fact]
        public void Nao_cancela_fechamento_e_mantem_api_ativa_sim_libera_porta_e_fdb()
        {
            EmSta(() =>
            {
                var container = CriarContainerReal();
                new DbInitializer(new Config { CaminhoFdb = _caminhoFdb }).Inicializar();
                var host = new ApiHost(container);
                int porta = PortaLivre();
                var timer = new TimerFake();
                bool resposta = false;
                bool instanciaLiberada = false;
                host.Start(porta, Pipeline);
                using (var form = new FrmConsulta(new ConsultaVendasPresenter(() => throw new InvalidOperationException())))
                {
                    var comp = new ComposicaoJanelaPrincipal(form, container, host, porta, _caminhoFdb, timer,
                        new AreaFake(), () => resposta, liberarInstancia: () => instanciaLiberada = true);
                    AguardarBancoOk(comp.Barra); // a checagem usa o .fdb de verdade

                    // "Não"
                    var nao = new FormClosingEventArgs(CloseReason.UserClosing, false);
                    comp.TratarFechando(nao);
                    Assert.True(nao.Cancel);
                    Assert.Equal(EstadoApiHost.Ativa, host.Estado);
                    Assert.False(comp.Encerramento.Encerrado);
                    Assert.False(timer.Parado);

                    // "Sim"
                    resposta = true;
                    var sim = new FormClosingEventArgs(CloseReason.UserClosing, false);
                    comp.TratarFechando(sim);
                    Assert.False(sim.Cancel);
                    comp.Encerramento.Encerrar();
                    comp.Encerramento.Encerrar(); // 2x: idempotente

                    Assert.True(timer.Parado);
                    Assert.True(timer.Descartado);
                    Assert.Equal(EstadoApiHost.Inativa, host.Estado);
                    Assert.True(instanciaLiberada);
                }

                // Porta liberada: novo HttpListener sobe.
                var l = new HttpListener();
                l.Prefixes.Add("http://localhost:" + porta + "/");
                l.Start();
                l.Stop();
                l.Close();

                // .fdb liberado: outro handle exclusivo e exclusão funcionam.
                using (new FileStream(_caminhoFdb, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                File.Delete(_caminhoFdb);
                Assert.False(File.Exists(_caminhoFdb));
            });
        }
    }
}
