using System;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Ponto de entrada do executável Desktop (esqueleto T-04).
    /// Composition root Autofac, splash/mutex de instância única (T-46) e
    /// inicialização do host OWIN (T-27) entram em tarefas posteriores.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            throw new NotImplementedException(
                "Esqueleto T-04: composition root, host OWIN e FrmConsulta chegam em T-26/T-27/T-42.");
        }
    }
}
