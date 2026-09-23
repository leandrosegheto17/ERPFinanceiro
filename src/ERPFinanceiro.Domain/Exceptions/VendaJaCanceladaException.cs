namespace ERPFinanceiro.Domain.Exceptions
{
    /// <summary>
    /// Lançada ao tentar transicionar (Quitar/Cancelar) uma venda cujo status já é
    /// Cancelada — estado terminal (RN-03/RN-04, T-08). A Api mapeia para 409.
    /// </summary>
    public class VendaJaCanceladaException : DomainException
    {
        public string VendaId { get; }

        public VendaJaCanceladaException(string vendaId)
            : base($"Venda '{vendaId}' já está cancelada; Cancelada é um estado terminal.")
        {
            VendaId = vendaId;
        }
    }
}
