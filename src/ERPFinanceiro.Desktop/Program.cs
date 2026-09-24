using System;
using System.Configuration;
using System.Windows.Forms;
using Autofac;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Infrastructure.Persistencia;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Ponto de entrada do Desktop (T-46, F-7). Fino de propósito: a decisão (mutex, banco, porta)
    /// vive em <see cref="SequenciaInicializacao"/> (testável sem UI); aqui só há splash, diálogos
    /// e <c>Application.Run</c>. Encerramento/confirmação ao fechar é T-47.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            ConfiguracaoVisual.AplicarAcessibilidadeDpi(); // T-51: antes de qualquer janela/handle
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            ConfiguracaoVisual.AplicarSkin(); // RL9-02: antes do primeiro Form (splash incluso)

            InstanciaUnica instancia = null;
            ApiHost host = null;
            IAppLogger loggerGlobal = null; // RL10-02: capturado antes do descarte do container
            var tratador = new TratadorExcecoesNaoTratadas(
                (contexto, ex) => RegistrarComFallback(loggerGlobal, contexto, ex),
                msg => XtraMessageBox.Show(msg, TextosInicializacao.TituloApp, MessageBoxButtons.OK, MessageBoxIcon.Error));
            tratador.Registrar();
            try
            {
                var container = new Lazy<IContainer>(CompositionRoot.Construir);
                host = null;

                var sequencia = new SequenciaInicializacao(
                    adquirirInstancia: () =>
                    {
                        instancia = InstanciaUnica.TentarAdquirir(InstanciaUnica.NomeMutexProducao);
                        return instancia != null;
                    },
                    inicializarBanco: () =>
                        new DbInitializer(container.Value.Resolve<IConfiguracaoBanco>()).Inicializar(),
                    iniciarApi: porta =>
                    {
                        host = new ApiHost(container.Value);
                        host.Start(porta);
                        return new ResultadoInicioApi(host.Estado, host.UltimoErro);
                    },
                    registrarLog: msg => container.Value.Resolve<IAppLogger>().Registrar(msg, "inicializacao"));

                int portaConfigurada = LerPorta();

                MostrarSplash();
                ResultadoInicializacao resultado;
                try
                {
                    resultado = sequencia.Executar(portaConfigurada);
                }
                finally
                {
                    FecharSplash();
                }

                switch (resultado.Decisao)
                {
                    case DecisaoInicializacao.SairSegundaInstancia:
                        XtraMessageBox.Show(TextosInicializacao.SegundaInstancia, TextosInicializacao.TituloApp,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return 1;

                    case DecisaoInicializacao.MostrarDialogoApi:
                        DialogResult escolha = XtraMessageBox.Show(TextosInicializacao.DialogoApi(resultado),
                            TextosInicializacao.TituloDialogoApi, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (escolha != DialogResult.Yes) return 1;
                        break;
                }

                // Banco em erro: a FrmConsulta abre normalmente e a carga (Shown -> Recarregar) cai no
                // estado de erro F-1 (T-43) porque o mesmo banco está indisponível; nunca fecha silencioso.
                IContainer raiz = container.Value;
                IAppLogger logger = raiz.Resolve<IAppLogger>();
                loggerGlobal = logger;
                Action<string, Exception> registrar = (contexto, ex) => RegistrarComFallback(logger, contexto, ex);

                var detalhe = new DetalheVendaPresenter(
                    DetalheVendaPresenter.CarregadorViaContainer(raiz), ex => registrar("Falha ao carregar detalhe da venda (F-3)", ex));
                var filtros = new FiltrosConsultaModelo(); // T-58: filtro corrente compartilhado por grid e relatório
                var consulta = new ConsultaVendasPresenter(
                    ConsultaVendasPresenter.CarregadorViaContainer(raiz, () => filtros.Corrente), ex => registrar("Falha ao carregar consulta de vendas (F-1)", ex));

                var janela = new FrmConsulta(consulta) { DetalhePresenter = detalhe };
                var inst = instancia;
                new ComposicaoJanelaPrincipal(janela, raiz, host ?? new ApiHost(raiz), portaConfigurada,
                    raiz.Resolve<IConfiguracaoBanco>().CaminhoFdb, new TemporizadorWinForms(),
                    new AreaTransferenciaWinForms(), ComposicaoJanelaPrincipal.ConfirmarComXtraMessageBox,
                    liberarInstancia: () => inst?.Dispose(),
                    aoFalhar: ex => registrar("Falha no encerramento", ex),
                    fluxoRelatorio: CriarFluxoRelatorio(raiz, registrar),
                    filtroCorrente: () => filtros.Corrente, filtros: filtros); // T-58: relatório usa o filtro real do painel

                System.Windows.Forms.Application.Run(janela);
                return 0;
            }
            catch (Exception ex)
            {
                // Falha antes/fora da sequência (ex.: App.config sem chave obrigatória): nunca sai calado.
                XtraMessageBox.Show("Não foi possível iniciar o aplicativo: " + ex.Message, TextosInicializacao.TituloApp,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 2;
            }
            finally
            {
                host?.Stop();
                instancia?.Dispose();
            }
        }

        /// <summary>
        /// RL10-02 (b): o container (e o TraceLogger) já pode ter sido descartado quando falha o passo do
        /// mutex; nesse caso a falha vai para o Trace do sistema em vez de se perder.
        /// </summary>
        internal static void RegistrarComFallback(IAppLogger logger, string contexto, Exception ex)
        {
            string texto = contexto + ": " + ex;
            try
            {
                if (logger == null) throw new InvalidOperationException("logger indisponível");
                logger.Registrar(texto, Guid.NewGuid().ToString("N"));
            }
            catch (Exception)
            {
                try { System.Diagnostics.Trace.TraceError(texto); } catch (Exception) { }
            }
        }

        /// <summary>T-50: fluxo F-5. Escopo Autofac novo por emissão (regra 5/RT-08); PDF gerado pelo Reports (T-49).</summary>
        private static FluxoEmitirRelatorio CriarFluxoRelatorio(IContainer raiz, Action<string, Exception> registrar)
        {
            bool desativado = FluxoEmitirRelatorio.LerPreviewDesativado(
                ConfigurationManager.AppSettings[TextosRelatorio.ChaveForcarSemPreview]);
            var gerador = new ERPFinanceiro.Reports.GeradorRelatorioPdf();
            return new FluxoEmitirRelatorio(
                obterDados: filtro =>
                {
                    using (ILifetimeScope escopo = raiz.BeginLifetimeScope())
                    {
                        return new ERPFinanceiro.Reports.RelatorioDataSource(escopo.Resolve<ERPFinanceiro.Application.Consultas.ConsultaService>()).Obter(filtro);
                    }
                },
                // D-03 (pendente) ainda nao decidida e T-62 pendente: linha pendente omitida (incluirPendente: false).
                gerarPdf: (dados, filtro, em, caminho) => gerador.Gerar(dados, filtro, em, caminho,
                    ERPFinanceiro.Reports.RelatorioDataSource.CalcularTotais(dados, incluirPendente: false)),
                pastaTemporaria: FluxoEmitirRelatorio.PastaTemporariaPadrao,
                abridor: new AbridorArquivoSistema(),
                preview: new ApresentadorPreviewIndisponivel(),
                agoraLocal: () => DateTime.Now,
                previewDesativado: desativado,
                aoFalhar: ex => registrar("Falha ao emitir relatório (F-5)", ex));
        }

        private static int LerPorta()
        {
            string valor = ConfigurationManager.AppSettings["Api:Porta"];
            if (!int.TryParse(valor, out int porta) || porta < 1 || porta > 65535)
            {
                throw new InvalidOperationException(
                    "Configuração inválida ou ausente: appSettings/Api:Porta (App.config). Veja App.config.example.");
            }

            return porta;
        }

        private static void MostrarSplash()
        {
            try { SplashScreenManager.ShowDefaultWaitForm(TextosInicializacao.TituloApp, TextosInicializacao.Splash); }
            catch (Exception) { /* splash é cosmético; falha dele não impede a inicialização */ }
        }

        private static void FecharSplash()
        {
            try { SplashScreenManager.CloseDefaultWaitForm(); }
            catch (Exception) { }
        }
    }
}
