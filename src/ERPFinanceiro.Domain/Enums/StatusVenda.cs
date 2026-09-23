namespace ERPFinanceiro.Domain.Enums
{
    /// <summary>
    /// Status da venda. Valores espelham FIN_VENDA.STATUS (database/01-schema.sql,
    /// CK_FIN_VENDA_STATUS) e SDD.md Seção 5.
    /// </summary>
    public enum StatusVenda
    {
        Pendente = 0,
        Quitada = 1,
        Cancelada = 2
    }
}
