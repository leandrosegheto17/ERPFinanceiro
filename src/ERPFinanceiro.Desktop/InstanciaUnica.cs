using System;
using System.Threading;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Mutex nomeado de instância única (F-7, TASK.md Seção 1 regra 11: um único processo abre o
    /// .fdb embarcado). Mantido até o encerramento do processo (<see cref="Dispose"/>).
    /// </summary>
    public sealed class InstanciaUnica : IDisposable
    {
        /// <summary>
        /// Nome do mutex de PRODUÇÃO. Prefixo <c>Global\</c> para valer entre sessões do Windows
        /// (o .fdb é único por máquina). Testes devem usar nomes próprios, nunca esta constante.
        /// </summary>
        public const string NomeMutexProducao = @"Global\ERPFinanceiro.Desktop.InstanciaUnica";

        private Mutex _mutex;

        private InstanciaUnica(Mutex mutex)
        {
            _mutex = mutex;
        }

        /// <summary>Fábrica de mutex (seam de teste): devolve o mutex e se foi criado por esta chamada.</summary>
        public delegate Mutex FabricaMutex(string nome, out bool criado);

        /// <summary>Devolve a posse do mutex, ou <c>null</c> se outro processo/instância já a detém.</summary>
        public static InstanciaUnica TentarAdquirir(string nome)
        {
            return TentarAdquirir(nome, (string n, out bool c) => new Mutex(true, n, out c));
        }

        /// <summary>
        /// Com fábrica injetável. <see cref="UnauthorizedAccessException"/> (mutex <c>Global\</c> criado por
        /// outro usuário/sessão) conta como segunda instância: devolve <c>null</c> (RL10-03).
        /// </summary>
        public static InstanciaUnica TentarAdquirir(string nome, FabricaMutex fabrica)
        {
            if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome do mutex obrigatório.", nameof(nome));
            if (fabrica == null) throw new ArgumentNullException(nameof(fabrica));

            Mutex mutex;
            bool criado;
            try
            {
                mutex = fabrica(nome, out criado);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            if (!criado)
            {
                mutex.Dispose();
                return null;
            }

            return new InstanciaUnica(mutex);
        }

        public void Dispose()
        {
            Mutex m = _mutex;
            _mutex = null;
            if (m == null) return;
            try { m.ReleaseMutex(); }
            catch (ApplicationException) { /* liberado por thread diferente da que adquiriu: o SO libera ao fim do processo */ }
            m.Dispose();
        }
    }
}
