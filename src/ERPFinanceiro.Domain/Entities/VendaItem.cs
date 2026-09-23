using System;

namespace ERPFinanceiro.Domain.Entities
{
    /// <summary>
    /// Item de uma venda. Espelha FIN_VENDA_ITEM (database/01-schema.sql):
    /// QUANTIDADE > 0, PRECO_UNITARIO >= 0 (CK_FIN_VENDA_ITEM_QTD/PRECO).
    /// </summary>
    public class VendaItem
    {
        /// <summary>PK técnica; atribuída pelo banco (identity). 0 enquanto não persistida.</summary>
        public long Id { get; internal set; }

        public string ProdutoId { get; private set; }

        public int Quantidade { get; private set; }

        public decimal PrecoUnitario { get; private set; }

        /// <summary>Uso exclusivo do EF (T-11)/testes. Não usar para criar item novo.</summary>
        protected VendaItem()
        {
        }

        public VendaItem(string produtoId, int quantidade, decimal precoUnitario)
        {
            if (string.IsNullOrWhiteSpace(produtoId))
            {
                throw new ArgumentException("ProdutoId é obrigatório.", nameof(produtoId));
            }

            if (quantidade <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantidade), quantidade, "Quantidade deve ser maior que zero.");
            }

            if (precoUnitario < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(precoUnitario), precoUnitario, "PrecoUnitario não pode ser negativo.");
            }

            ProdutoId = produtoId;
            Quantidade = quantidade;
            PrecoUnitario = precoUnitario;
        }
    }
}
