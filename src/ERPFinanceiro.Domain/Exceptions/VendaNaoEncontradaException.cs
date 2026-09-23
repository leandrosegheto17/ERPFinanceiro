namespace ERPFinanceiro.Domain.Exceptions
{
    /// <summary>
    /// Lançada quando uma operação referencia uma venda que não existe na base
    /// (ex.: <c>GET /api/vendas/{vendaId}/status</c>, T-31/T-24). A Api mapeia
    /// para 404 <c>VENDA_NAO_ENCONTRADA</c> (contrato-v1.1.md Seção 2).
    /// <para>
    /// Criada por antecipação em T-28 (`ExceptionHandler` global): o handler
    /// precisa de um tipo concreto para mapear -> 404 antes de as tarefas de
    /// serviço que a lançariam de fato existirem (T-18/T-19/T-24, ainda não
    /// implementadas neste momento). Segue o mesmo padrão de
    /// <see cref="VendaJaCanceladaException"/>/<see cref="MotivoObrigatorioException"/>.
    /// Se T-18/T-19/T-24 concluírem que a semântica deveria carregar dado
    /// adicional, o construtor pode ser estendido por essas tarefas sem impacto
    /// no mapeamento em T-28 (que só depende do tipo e de <see cref="System.Exception.Message"/>).
    /// </para>
    /// </summary>
    public class VendaNaoEncontradaException : DomainException
    {
        public string VendaId { get; }

        public VendaNaoEncontradaException(string vendaId)
            : base($"Venda '{vendaId}' não encontrada.")
        {
            VendaId = vendaId;
        }
    }
}
