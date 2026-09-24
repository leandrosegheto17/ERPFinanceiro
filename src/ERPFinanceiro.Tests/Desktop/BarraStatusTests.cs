using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>T-45: lógica da barra de status F-6 (presenter) e controles visuais headless.</summary>
    public class BarraStatusTests
    {
        private sealed class FonteFake : IFonteEstadoApi
        {
            public EstadoApiHost Estado { get; set; } = EstadoApiHost.Ativa;
            public Exception UltimoErro { get; set; }
            public string EnderecoBase { get; set; } = "http://localhost:5000/";
        }

        private sealed class HealthFake : IHealthService
        {
            public ResultadoHealth Resultado = ResultadoHealth.ComSucesso();
            public int Chamadas;
            public ManualResetEventSlim Segurar;
            public ResultadoHealth ObterStatus()
            {
                Interlocked.Increment(ref Chamadas);
                Segurar?.Wait();
                return Resultado;
            }
        }

        private sealed class AreaFake : IAreaTransferencia
        {
            public string Texto;
            public void Copiar(string texto) { Texto = texto; }
        }

        private sealed class TimerFake : ITemporizador
        {
            public event EventHandler Tick;
            public TimeSpan? Intervalo;
            public bool Parado;
            public void Iniciar(TimeSpan i) { Intervalo = i; }
            public void Parar() { Parado = true; }
            public void Dispose() { }
            public void Disparar() { Tick?.Invoke(this, EventArgs.Empty); }
        }

        private static BarraStatusPresenter Criar(FonteFake api, HealthFake h, AreaFake a = null, TimerFake t = null)
            => new BarraStatusPresenter(api, h, 5000, @"C:\dados\x.fdb", a ?? new AreaFake(), t ?? new TimerFake());

        [Fact]
        public async Task Ativa_e_banco_ok_mostram_icone_e_texto()
        {
            var p = Criar(new FonteFake(), new HealthFake());
            await p.AtualizarAsync();
            Assert.Equal("API: Ativa (http://localhost:5000)", p.Api.Texto);
            Assert.Equal(IconeStatus.Check, p.Api.Icone);
            Assert.Equal("Banco: OK", p.Banco.Texto);
            Assert.Equal(IconeStatus.Check, p.Banco.Icone);
        }

        [Fact]
        public async Task Indicadores_sao_independentes()
        {
            var api = new FonteFake { Estado = EstadoApiHost.InativaPortaEmUso, UltimoErro = new HttpListenerException(183, "porta ocupada") };
            var h = new HealthFake { Resultado = ResultadoHealth.ComSucesso() };
            var p = Criar(api, h);
            await p.AtualizarAsync();
            Assert.Equal("API: Inativa - porta em uso", p.Api.Texto);
            Assert.Equal(IconeStatus.X, p.Api.Icone);
            Assert.Equal("Banco: OK", p.Banco.Texto);

            api.Estado = EstadoApiHost.Ativa;
            h.Resultado = ResultadoHealth.ComFalha("arquivo bloqueado");
            await p.AtualizarAsync();
            Assert.StartsWith("API: Ativa", p.Api.Texto);
            Assert.Equal("Banco: indisponível", p.Banco.Texto);
            Assert.Equal(IconeStatus.X, p.Banco.Icone);
        }

        [Fact]
        public void Estados_iniciais_api_iniciando_e_inativa_e_banco_verificando()
        {
            var api = new FonteFake { Estado = EstadoApiHost.Iniciando };
            var p = Criar(api, new HealthFake());
            Assert.Equal("API: Iniciando...", p.Api.Texto);
            Assert.Equal(IconeStatus.Relogio, p.Api.Icone);
            Assert.Equal("Banco: verificando...", p.Banco.Texto);
        }

        [Fact]
        public async Task Timer_de_15s_reflete_queda_do_banco_sem_esperar_15s_reais()
        {
            var t = new TimerFake();
            var h = new HealthFake();
            var p = Criar(new FonteFake(), h, null, t);
            p.Iniciar();
            Assert.Equal(TimeSpan.FromSeconds(15), t.Intervalo);
            await Esperar(() => p.Banco.Texto == "Banco: OK");

            h.Resultado = ResultadoHealth.ComFalha("caiu");
            t.Disparar(); // um tick de 15 s
            await Esperar(() => p.Banco.Texto == "Banco: indisponível");
            Assert.Equal("Banco: indisponível", p.Banco.Texto);
        }

        [Fact]
        public async Task Nao_empilha_checagens_enquanto_a_anterior_nao_terminou()
        {
            var h = new HealthFake { Segurar = new ManualResetEventSlim(false) };
            var p = Criar(new FonteFake(), h);
            var primeira = p.AtualizarAsync();
            await Esperar(() => h.Chamadas == 1);
            await p.AtualizarAsync(); // ignorada
            await p.AtualizarAsync(); // ignorada
            Assert.Equal(1, h.Chamadas);
            h.Segurar.Set();
            await primeira;
            await p.AtualizarAsync();
            Assert.Equal(2, h.Chamadas);
        }

        [Fact]
        public async Task Excecao_no_health_vira_banco_indisponivel()
        {
            var p = new BarraStatusPresenter(new FonteFake(), new ThrowingHealth(), 5000, "x", new AreaFake(), new TimerFake());
            await p.AtualizarAsync();
            Assert.Equal("Banco: indisponível", p.Banco.Texto);
            Assert.Contains("boom", p.UltimoErro);
        }

        [Theory]
        [InlineData("Erro ao abrir DataSource=x;Password=masterkey;Database=a.fdb", "masterkey")]
        [InlineData("falha User=SYSDBA; PWD=segredo123 timeout", "segredo123")]
        public async Task Ultimo_erro_e_texto_copiado_mascaram_credenciais(string msg, string segredo)
        {
            var area = new AreaFake();
            var api = new FonteFake { UltimoErro = new InvalidOperationException(msg) };
            var health = new HealthFake { Resultado = ResultadoHealth.ComFalha(msg) };
            var p = new BarraStatusPresenter(api, health, 5000, "x", area, new TimerFake());
            await p.AtualizarAsync();
            p.CopiarDetalhes();
            Assert.DoesNotContain(segredo, area.Texto);
            Assert.DoesNotContain("SYSDBA", area.Texto);
            Assert.Contains("***", area.Texto);
        }

        [Fact]
        public async Task Ultimo_erro_limita_tamanho()
        {
            var api = new FonteFake { UltimoErro = new InvalidOperationException(new string('a', 5000)) };
            var p = new BarraStatusPresenter(api, new HealthFake(), 5000, "x", new AreaFake(), new TimerFake());
            Assert.True(p.UltimoErro.Length < 600);
        }

        private sealed class ThrowingHealth : IHealthService
        {
            public ResultadoHealth ObterStatus() { throw new InvalidOperationException("boom"); }
        }

        [Fact]
        public async Task Detalhes_e_copiar_incluem_porta_caminho_e_ultimo_erro()
        {
            var api = new FonteFake { Estado = EstadoApiHost.InativaPortaEmUso, UltimoErro = new HttpListenerException(183, "porta ocupada") };
            var area = new AreaFake();
            var p = Criar(api, new HealthFake { Resultado = ResultadoHealth.ComFalha("sem acesso ao arquivo") }, area);
            await p.AtualizarAsync();
            p.CopiarDetalhes();
            Assert.Contains("Porta: 5000", area.Texto);
            Assert.Contains(@"C:\dados\x.fdb", area.Texto);
            Assert.Contains("porta ocupada", area.Texto);
            Assert.Contains("sem acesso ao arquivo", area.Texto);
        }

        [Fact]
        public async Task Sem_erro_diz_nenhum()
        {
            var p = Criar(new FonteFake(), new HealthFake());
            await p.AtualizarAsync();
            Assert.Contains("Nenhum erro registrado.", p.TextoDetalhes);
        }

        [Fact]
        public void Controles_visuais_headless_mostram_icone_e_texto_e_copiar_usa_a_abstracao()
        {
            Exception erro = null;
            var th = new Thread(() =>
            {
                try
                {
                    var area = new AreaFake();
                    var p = Criar(new FonteFake(), new HealthFake(), area);
                    using (var form = new DevExpress.XtraEditors.XtraForm())
                    using (var barra = new BarraStatusVisual(form, p))
                    {
                        Assert.Equal("API: Ativa (http://localhost:5000)", barra.TextoApi);
                        Assert.True(barra.ApiTemIcone);
                        Assert.Equal("Banco: verificando...", barra.TextoBanco);
                        Assert.True(barra.BancoTemIcone);
                        using (var dlg = new FrmDetalhesStatus(p))
                        {
                            Assert.Contains("Porta: 5000", dlg.TextoExibido);
                            dlg.Copiar();
                        }
                        Assert.Contains("Porta: 5000", area.Texto);
                    }
                }
                catch (Exception ex) { erro = ex; }
            });
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
            th.Join();
            if (erro != null) throw new Exception("Falha na thread STA: " + erro, erro);
        }

        private static async Task Esperar(Func<bool> cond)
        {
            for (int i = 0; i < 200 && !cond(); i++) await Task.Delay(25);
            Assert.True(cond(), "condição não atingida a tempo");
        }
    }
}
