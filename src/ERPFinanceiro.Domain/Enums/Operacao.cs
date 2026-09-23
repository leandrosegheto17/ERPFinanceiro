namespace ERPFinanceiro.Domain.Enums
{
    /// <summary>
    /// Operação registrada em FIN_VENDA_HISTORICO.OPERACAO (database/01-schema.sql,
    /// CK_FIN_VENDA_HIST_OPER) e SDD.md Seção 5.
    /// </summary>
    public enum Operacao
    {
        Recebida = 0,
        Quitacao = 1,
        Cancelamento = 2
    }
}
