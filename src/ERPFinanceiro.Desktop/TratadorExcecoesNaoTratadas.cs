using System;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// RL10-02 (a): exceção não tratada de UI (Application.ThreadException) ou de AppDomain gera log
    /// e mensagem amigável sem stack. Sem UI: log e diálogo são injetados (testável).
    /// </summary>
    public sealed class TratadorExcecoesNaoTratadas
    {
        public const string TextoMensagem = "Ocorreu um erro inesperado. Detalhes foram registrados no log.";

        private readonly Action<string, Exception> _registrar;
        private readonly Action<string> _mostrar;

        public TratadorExcecoesNaoTratadas(Action<string, Exception> registrar, Action<string> mostrar)
        {
            _registrar = registrar ?? ((c, e) => { });
            _mostrar = mostrar ?? (m => { });
        }

        public void Tratar(string contexto, Exception ex)
        {
            try { _registrar(contexto, ex); } catch (Exception) { }
            try { _mostrar(TextoMensagem); } catch (Exception) { }
        }

        /// <summary>Registra os dois handlers globais (chamar antes de Application.Run).</summary>
        public void Registrar()
        {
            System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);
            System.Windows.Forms.Application.ThreadException += (s, e) => Tratar("Exceção não tratada de UI", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Tratar("Exceção não tratada de AppDomain", e.ExceptionObject as Exception ?? new Exception(Convert.ToString(e.ExceptionObject)));
        }
    }
}
