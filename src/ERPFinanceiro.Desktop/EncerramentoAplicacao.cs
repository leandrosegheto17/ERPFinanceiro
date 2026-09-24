using System;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Ciclo de encerramento (T-47, UX-SPEC F-7/ADR-004). Sem UI: a decisão de fechar e a ordem
    /// (barra/timer -> API -> container/.fdb -> instância única) são testáveis por xUnit.
    /// Idempotente: chamar <see cref="Encerrar"/> 2x executa cada passo uma única vez.
    /// Um passo que falha não impede os seguintes (não deixar porta/.fdb presos).
    /// </summary>
    public sealed class EncerramentoAplicacao
    {
        public const string TextoConfirmacao = "Fechar encerra a API; o ERP Vendas não conseguirá enviar vendas.";

        private readonly Action _pararBarra;
        private readonly Action _pararApi;
        private readonly Action _descartarContainer;
        private readonly Action _liberarInstancia;
        private readonly Action<Exception> _aoFalhar;
        private readonly object _sync = new object();

        public EncerramentoAplicacao(Action pararBarra, Action pararApi, Action descartarContainer,
            Action liberarInstancia = null, Action<Exception> aoFalhar = null)
        {
            _pararBarra = pararBarra ?? (() => { });
            _pararApi = pararApi ?? (() => { });
            _descartarContainer = descartarContainer ?? (() => { });
            _liberarInstancia = liberarInstancia ?? (() => { });
            _aoFalhar = aoFalhar ?? (_ => { });
        }

        public bool Encerrado { get; private set; }

        /// <summary>Já encerrado fecha sem perguntar; senão só fecha se o usuário confirmar ("Não" = false).</summary>
        public bool PodeFechar(Func<bool> confirmar)
        {
            if (Encerrado) return true;
            return confirmar != null && confirmar();
        }

        /// <summary>Barra/timer -> ApiHost.Stop (porta) -> container (.fdb) -> mutex. Idempotente.</summary>
        public void Encerrar()
        {
            lock (_sync)
            {
                if (Encerrado) return;
                Encerrado = true;
                Executar(_pararBarra);
                Executar(_pararApi);
                Executar(_descartarContainer);
                Executar(_liberarInstancia);
            }
        }

        private void Executar(Action passo)
        {
            try { passo(); }
            catch (Exception ex) { try { _aoFalhar(ex); } catch (Exception) { } }
        }
    }
}
