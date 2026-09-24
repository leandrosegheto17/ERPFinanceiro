using System;

namespace ERPFinanceiro.Desktop
{
    /// <summary>Resultado do bind da API, devolvido ao delegado de início (produção: lê <see cref="ApiHost"/>).</summary>
    public sealed class ResultadoInicioApi
    {
        public ResultadoInicioApi(EstadoApiHost estado, Exception erro)
        {
            Estado = estado;
            Erro = erro;
        }

        public EstadoApiHost Estado { get; }
        public Exception Erro { get; }
    }

    /// <summary>Decisão tipada que o <c>Program</c> executa (F-7, UX-SPEC 2.7).</summary>
    public enum DecisaoInicializacao
    {
        /// <summary>Banco e API OK: abre <c>FrmConsulta</c> normalmente.</summary>
        AbrirNormal,

        /// <summary>Mutex já detido por outro processo: avisa e sai, sem tocar no .fdb.</summary>
        SairSegundaInstancia,

        /// <summary>Banco falhou: abre <c>FrmConsulta</c> em estado de erro (F-1 Erro) — nunca fecha silencioso.</summary>
        AbrirEmEstadoDeErroDeBanco,

        /// <summary>API não subiu (porta em uso ou falha de host): diálogo com causa e ação; usuário decide Continuar/Sair.</summary>
        MostrarDialogoApi
    }

    /// <summary>Resultado da <see cref="SequenciaInicializacao"/>: dados suficientes para o Program decidir o que mostrar.</summary>
    public sealed class ResultadoInicializacao
    {
        public ResultadoInicializacao(bool segundaInstancia, Exception erroBanco, EstadoApiHost? estadoApi, Exception erroApi, int porta)
        {
            SegundaInstancia = segundaInstancia;
            ErroBanco = erroBanco;
            EstadoApi = estadoApi;
            ErroApi = erroApi;
            Porta = porta;
        }

        public bool SegundaInstancia { get; }
        public Exception ErroBanco { get; }
        public bool BancoEmErro => ErroBanco != null;
        /// <summary>Nulo quando a API nem foi tentada (segunda instância).</summary>
        public EstadoApiHost? EstadoApi { get; }
        public Exception ErroApi { get; }
        public int Porta { get; }

        public bool PortaEmUso => EstadoApi == EstadoApiHost.InativaPortaEmUso;

        /// <summary>API tentada mas não ativa (porta em uso ou outra falha de host).</summary>
        public bool ApiFalhou => !SegundaInstancia && EstadoApi != EstadoApiHost.Ativa;

        /// <summary>
        /// Ordem de precedência: segunda instância sai; falha de API exige diálogo (antes de abrir a
        /// janela, que segue em estado de erro se o banco também falhou); depois banco em erro; senão normal.
        /// </summary>
        public DecisaoInicializacao Decisao
        {
            get
            {
                if (SegundaInstancia) return DecisaoInicializacao.SairSegundaInstancia;
                if (ApiFalhou) return DecisaoInicializacao.MostrarDialogoApi;
                if (BancoEmErro) return DecisaoInicializacao.AbrirEmEstadoDeErroDeBanco;
                return DecisaoInicializacao.AbrirNormal;
            }
        }
    }

    /// <summary>
    /// Orquestração da inicialização F-7 (T-46), sem UI: (1) instância única; (2) banco
    /// (<c>DbInitializer</c>); (3) API (<c>ApiHost.Start</c>). Dependências injetadas como
    /// delegates para teste xUnit com Firebird/porta reais, sem formulários. A API é tentada mesmo
    /// com o banco em erro, para que a barra de status (F-6) reporte "Banco: indisponível" e o
    /// <c>/health</c> responda 503, em vez de esconder a causa. Segunda instância nunca toca no
    /// banco nem na porta.
    /// </summary>
    public sealed class SequenciaInicializacao
    {
        private readonly Func<bool> _adquirirInstancia;
        private readonly Action _inicializarBanco;
        private readonly Func<int, ResultadoInicioApi> _iniciarApi;
        private readonly Action<string> _registrarLog;

        public SequenciaInicializacao(
            Func<bool> adquirirInstancia,
            Action inicializarBanco,
            Func<int, ResultadoInicioApi> iniciarApi,
            Action<string> registrarLog = null)
        {
            _adquirirInstancia = adquirirInstancia ?? throw new ArgumentNullException(nameof(adquirirInstancia));
            _inicializarBanco = inicializarBanco ?? throw new ArgumentNullException(nameof(inicializarBanco));
            _iniciarApi = iniciarApi ?? throw new ArgumentNullException(nameof(iniciarApi));
            _registrarLog = registrarLog;
        }

        public ResultadoInicializacao Executar(int porta)
        {
            if (!_adquirirInstancia())
            {
                return new ResultadoInicializacao(true, null, null, null, porta);
            }

            Exception erroBanco = null;
            try
            {
                _inicializarBanco();
            }
            catch (Exception ex)
            {
                erroBanco = ex;
                Log("Falha na inicialização do banco: " + ex);
            }

            EstadoApiHost estadoApi;
            Exception erroApi = null;
            try
            {
                ResultadoInicioApi r = _iniciarApi(porta);
                estadoApi = r.Estado;
                erroApi = r.Erro;
            }
            catch (Exception ex)
            {
                estadoApi = EstadoApiHost.Inativa;
                erroApi = ex;
            }

            if (estadoApi != EstadoApiHost.Ativa)
            {
                Log("API não iniciou na porta " + porta + " (" + estadoApi + "): " + erroApi);
            }

            return new ResultadoInicializacao(false, erroBanco, estadoApi, erroApi, porta);
        }

        private void Log(string mensagem)
        {
            try { _registrarLog?.Invoke(mensagem); }
            catch (Exception) { /* log é melhor esforço */ }
        }
    }
}
