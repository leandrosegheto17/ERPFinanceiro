using System;
using System.Threading.Tasks;
using ERPFinanceiro.Application.Interfaces;

namespace ERPFinanceiro.Desktop
{
    /// <summary>Fonte do estado da API em processo (implementada sobre <see cref="ApiHost"/>).</summary>
    public interface IFonteEstadoApi
    {
        EstadoApiHost Estado { get; }
        Exception UltimoErro { get; }
        string EnderecoBase { get; }
    }

    /// <summary>Adaptador de <see cref="ApiHost"/> para <see cref="IFonteEstadoApi"/>.</summary>
    public sealed class FonteEstadoApiHost : IFonteEstadoApi
    {
        private readonly ApiHost _host;
        public FonteEstadoApiHost(ApiHost host) { _host = host ?? throw new ArgumentNullException(nameof(host)); }
        public EstadoApiHost Estado => _host.Estado;
        public Exception UltimoErro => _host.UltimoErro;
        public string EnderecoBase => _host.EnderecoBase;
    }

    /// <summary>Área de transferência abstraída para teste sem <c>Clipboard</c> real (exige STA).</summary>
    public interface IAreaTransferencia
    {
        void Copiar(string texto);
    }

    /// <summary>Implementação real (System.Windows.Forms.Clipboard; exige thread STA/UI).</summary>
    public sealed class AreaTransferenciaWinForms : IAreaTransferencia
    {
        public void Copiar(string texto)
        {
            System.Windows.Forms.Clipboard.SetText(texto ?? string.Empty);
        }
    }

    /// <summary>Temporizador abstraído; o real dispara <see cref="Tick"/> na UI thread.</summary>
    public interface ITemporizador : IDisposable
    {
        event EventHandler Tick;
        void Iniciar(TimeSpan intervalo);
        void Parar();
    }

    /// <summary>Temporizador real baseado em <c>System.Windows.Forms.Timer</c> (Tick na UI thread).</summary>
    public sealed class TemporizadorWinForms : ITemporizador
    {
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
        public event EventHandler Tick;

        public TemporizadorWinForms()
        {
            _timer.Tick += (s, e) => Tick?.Invoke(this, EventArgs.Empty);
        }

        public void Iniciar(TimeSpan intervalo)
        {
            _timer.Interval = (int)intervalo.TotalMilliseconds;
            _timer.Start();
        }

        public void Parar() => _timer.Stop();
        public void Dispose() => _timer.Dispose();
    }

    /// <summary>Um indicador: ícone + texto (nunca só cor).</summary>
    public sealed class IndicadorStatus
    {
        public IndicadorStatus(IconeStatus icone, string texto)
        {
            Icone = icone;
            Texto = texto;
        }

        public IconeStatus Icone { get; }
        public string Texto { get; }
    }

    /// <summary>
    /// Lógica da barra de status F-6 (T-45, UX-SPEC 2.4): API e Banco independentes, atualizados
    /// na abertura, por temporizador (15 s) e sob demanda. O health check roda fora da UI thread
    /// (regra 5) e não empilha: se a checagem anterior não terminou, a nova é ignorada.
    /// </summary>
    public sealed class BarraStatusPresenter : IDisposable
    {
        public static readonly TimeSpan IntervaloPadrao = TimeSpan.FromSeconds(15);

        private readonly IFonteEstadoApi _api;
        private readonly IHealthService _health;
        private readonly IAreaTransferencia _areaTransferencia;
        private readonly ITemporizador _temporizador;
        private readonly object _sync = new object();
        private bool _emAndamento;
        private ResultadoHealth _ultimoHealth;

        public BarraStatusPresenter(IFonteEstadoApi api, IHealthService health, int porta, string caminhoFdb,
            IAreaTransferencia areaTransferencia, ITemporizador temporizador)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _health = health ?? throw new ArgumentNullException(nameof(health));
            _areaTransferencia = areaTransferencia ?? throw new ArgumentNullException(nameof(areaTransferencia));
            _temporizador = temporizador ?? throw new ArgumentNullException(nameof(temporizador));
            Porta = porta;
            CaminhoFdb = caminhoFdb;
            Api = MontarApi();
            Banco = new IndicadorStatus(IconeStatus.Relogio, "Banco: verificando...");
            _temporizador.Tick += (s, e) => { var _ = AtualizarAsync(); };
        }

        public int Porta { get; }
        public string CaminhoFdb { get; }
        public IndicadorStatus Api { get; private set; }
        public IndicadorStatus Banco { get; private set; }

        /// <summary>Disparado após cada atualização (na thread que aguardou; UI thread quando há SynchronizationContext).</summary>
        public event EventHandler Alterado;

        /// <summary>Atualiza imediatamente e passa a atualizar a cada 15 s.</summary>
        public void Iniciar()
        {
            _temporizador.Iniciar(IntervaloPadrao);
            var _ = AtualizarAsync();
        }

        public void Dispose()
        {
            _temporizador.Parar();
            _temporizador.Dispose();
        }

        /// <summary>Atualiza API (leitura em memória) e Banco (Task.Run, sem bloquear a UI). Ignora se já em andamento.</summary>
        public async Task AtualizarAsync()
        {
            lock (_sync)
            {
                if (_emAndamento) return;
                _emAndamento = true;
            }

            try
            {
                Api = MontarApi();
                Alterado?.Invoke(this, EventArgs.Empty);

                ResultadoHealth resultado;
                try
                {
                    resultado = await Task.Run(() => _health.ObterStatus());
                }
                catch (Exception ex)
                {
                    resultado = ResultadoHealth.ComFalha(ex.Message);
                }

                _ultimoHealth = resultado;
                Api = MontarApi();
                Banco = resultado.Ok
                    ? new IndicadorStatus(IconeStatus.Check, "Banco: OK")
                    : new IndicadorStatus(IconeStatus.X, "Banco: indisponível");
                Alterado?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                lock (_sync) { _emAndamento = false; }
            }
        }

        private IndicadorStatus MontarApi()
        {
            switch (_api.Estado)
            {
                case EstadoApiHost.Ativa:
                    string end = (_api.EnderecoBase ?? string.Empty).TrimEnd('/');
                    return new IndicadorStatus(IconeStatus.Check, string.IsNullOrEmpty(end) ? "API: Ativa" : "API: Ativa (" + end + ")");
                case EstadoApiHost.Iniciando:
                    return new IndicadorStatus(IconeStatus.Relogio, "API: Iniciando...");
                case EstadoApiHost.InativaPortaEmUso:
                    return new IndicadorStatus(IconeStatus.X, "API: Inativa - porta em uso");
                default:
                    return new IndicadorStatus(IconeStatus.X, "API: Inativa");
            }
        }

        /// <summary>Texto do último erro conhecido (API e/ou Banco); "Nenhum erro registrado." se não houver.</summary>
        public string UltimoErro
        {
            get
            {
                var partes = new System.Collections.Generic.List<string>();
                if (_api.UltimoErro != null) partes.Add("API: " + _api.UltimoErro.Message);
                var h = _ultimoHealth;
                if (h != null && !h.Ok) partes.Add("Banco: " + h.Mensagem);
                return partes.Count == 0 ? "Nenhum erro registrado." : string.Join(Environment.NewLine, partes);
            }
        }

        /// <summary>Texto do diálogo de detalhes (porta, caminho do .fdb, último erro) — também o texto copiado.</summary>
        public string TextoDetalhes =>
            "Porta: " + Porta + Environment.NewLine +
            "Caminho do banco: " + CaminhoFdb + Environment.NewLine +
            "Último erro: " + UltimoErro;

        /// <summary>Ação do botão Copiar.</summary>
        public void CopiarDetalhes() => _areaTransferencia.Copiar(TextoDetalhes);
    }
}
