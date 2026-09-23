namespace ERPFinanceiro.Domain.Exceptions
{
    /// <summary>
    /// Lançada quando o motivo de cancelamento é obrigatório e não foi informado.
    /// Classe criada em T-07; regra S-05 (Venda.Cancelar sobre venda Quitada exige
    /// motivo não vazio) implementada em T-09.
    /// </summary>
    public class MotivoObrigatorioException : DomainException
    {
        public MotivoObrigatorioException()
            : base("Motivo do cancelamento é obrigatório.")
        {
        }
    }
}
