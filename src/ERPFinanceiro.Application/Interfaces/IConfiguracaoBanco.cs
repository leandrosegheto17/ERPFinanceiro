namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Abstração de configuração do banco embarcado (T-12). Implementada na
    /// Infrastructure/composition root a partir do <c>App.config</c>
    /// (<c>Banco:CaminhoFdb</c>/<c>Banco:Usuario</c>/<c>Banco:Senha</c>, ver
    /// App.config.example) — nunca lida diretamente pelo Domain/Application,
    /// só recebida via injeção (regra "credenciais por configuração, não
    /// hardcoded", TASK.md T-12).
    /// </summary>
    public interface IConfiguracaoBanco
    {
        /// <summary>Caminho completo do arquivo .fdb embarcado.</summary>
        string CaminhoFdb { get; }

        /// <summary>Usuário Firebird (SYSDBA por padrão).</summary>
        string Usuario { get; }

        /// <summary>Senha do usuário Firebird.</summary>
        string Senha { get; }
    }
}
