namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Resultado tipado de <see cref="IHealthService.ObterStatus"/> (T-22). Nunca é
    /// construído a partir de uma exceção não tratada propagada ao chamador —
    /// <see cref="Ok"/> encapsula qualquer falha de conexão/consulta numa mensagem
    /// resumida (<see cref="Mensagem"/>), sem stack trace (consumida pela API em
    /// T-36 no corpo do `GET /api/health` e pela tela em T-45 via
    /// <see cref="IHealthService"/> em processo — SDD.md Seção 2.6).
    /// </summary>
    public sealed class ResultadoHealth
    {
        private ResultadoHealth(bool ok, string mensagem)
        {
            Ok = ok;
            Mensagem = mensagem;
        }

        /// <summary>true quando a consulta de verificação (`SELECT 1`) executou com sucesso.</summary>
        public bool Ok { get; }

        /// <summary>
        /// Mensagem resumida do estado. Em sucesso, texto fixo curto; em falha, resumo
        /// da causa (sem stack trace) — nunca <c>null</c>.
        /// </summary>
        public string Mensagem { get; }

        public static ResultadoHealth ComSucesso(string mensagem = "ok")
        {
            return new ResultadoHealth(true, mensagem ?? "ok");
        }

        public static ResultadoHealth ComFalha(string mensagem)
        {
            return new ResultadoHealth(false, string.IsNullOrWhiteSpace(mensagem) ? "falha" : mensagem);
        }
    }
}
